using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using api.Dtos.Comment;
using api.Extensions;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/comment")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentRepository _commentRepo;
        private readonly IStockRespository _stockRepo;
        private readonly UserManager<AppUser> _userManager;
        private readonly IFMPService _fmpService;

        public CommentController(ICommentRepository commentRepo, IStockRespository stockRepo, UserManager<AppUser> userManager, IFMPService fmpService)
        {
            _commentRepo = commentRepo;
            _stockRepo = stockRepo;
            _userManager = userManager;
            _fmpService = fmpService;
        }

        [HttpGet]
       public async Task<IActionResult> GetAll([FromQuery] CommentQueryObject queryObject)
        {
            //Data Validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var comments = await _commentRepo.GetAllAsync(queryObject);
            var commentDtos = comments.Select(s => s.ToCommentDto());
            return Ok(commentDtos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            //Data Validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            var comment = await _commentRepo.GetByIdAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            return Ok(comment.ToCommentDto());
        }

        [HttpPost("{symbol:alpha}")]
        public async Task<IActionResult> Create(string symbol, [FromBody] CreateCommentDto commentDto)
        {
            // Validate input DTO
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Try to get stock from local DB
            var stock = await _stockRepo.GetBySymbolAsync(symbol);

            // If not found, fetch from FMP API
            if (stock == null)
            {
                stock = await _fmpService.FindStockBySymbolAsync(symbol);
                
                // If still not found → reject
                if (stock == null)
                    return BadRequest("Stock does not exist");

                // Cache the new stock in DB
                await _stockRepo.CreateAsync(stock);
            }

            // Get current user from JWT and lookup ID
            var user = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(user);

            // Map DTO to entity and link to stock & user
            var commentModel = commentDto.ToCommentFromCreate(stock.Id);
            commentModel.AppUserId = appUser.Id;

            // Save comment
            await _commentRepo.CreateAsync(commentModel);

            // Return 201 with location and DTO
            return CreatedAtAction(nameof(GetById), new { id = commentModel.Id }, commentModel.ToCommentDto());
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCommentRequestDto updateDto)
        {
            //Data Validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var comment = await _commentRepo.UpdateAsync(id, updateDto.ToCommentFromUpdate(id));
            if (comment == null)
            {
                return NotFound();
            }
            return Ok(comment.ToCommentDto());
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            //Data Validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            var commentModel = await _commentRepo.DeleteAsync(id);

            if (commentModel == null)
            {
                return NotFound("Comment does not exist");
            }

            return Ok(commentModel);

        }
    }
}