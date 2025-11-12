using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Dtos.Stock;
using Microsoft.Identity.Client;
using api.Interfaces;
using api.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers
{
    // REST API usage:
    // - Resource: "api/stock"
    // - HTTP verbs & behavior:
    //     GET    /api/stock        -> list stocks (200 OK)
    //     GET    /api/stock/{id}   -> get stock by id (200 OK) or 404 NotFound
    //     POST   /api/stock        -> create stock (201 Created, Location header via CreatedAtAction)
    //     PUT    /api/stock/{id}   -> update stock (200 OK) or 404 NotFound
    //     DELETE /api/stock/{id}   -> delete stock (204 NoContent) or 404 NotFound
    // - Uses DTOs for input/output (CreateSTockRequestDto, ToStockDto)
    // - Model binding: [FromBody] for POST
    // - Persistence: EF Core DbContext (_context) with SaveChanges
    [Route("api/stock")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IStockRespository _stockRepo;
        public StockController(IStockRespository stockRepo)
        {
            _stockRepo = stockRepo;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            //Data Validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var stocks = await _stockRepo.GetAllAsync(query);
            var stockDtos = stocks.Select(s => s.ToStockDto()).ToList();
            return Ok(stockDtos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {

            //Data Validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var stock = await _stockRepo.GetByIdAsync(id);
            if (stock == null)
            {
                return NotFound();
            }
            return Ok(stock.ToStockDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStockRequestDto stockDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var stockModel = stockDto.ToStockFromCreateDTO();

            await _stockRepo.CreateAsync(stockModel);

            return CreatedAtAction(nameof(GetById), new { id = stockModel.Id }, stockModel.ToStockDto());
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStockRequestDto updateDto)
        {

            //Data Validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var stock = await _stockRepo.UpdateAsync(id, updateDto);
            if (stock == null)
            {
                return NotFound();
            }
            return Ok(stock.ToStockDto());
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            //Data Validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deleted = await _stockRepo.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}