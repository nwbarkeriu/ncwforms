/*
 * EmailController - API endpoint for sending sprint status emails
 * Formats HTML table data into styled emails with project-specific recipients
 */
using Microsoft.AspNetCore.Mvc;
using JobCompare.Services;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;

    public EmailController(EmailService emailService)
    {
        _emailService = emailService;
    }
    
    // POST api/email - Sends formatted sprint status email with HTML table
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] EmailRequest request)
    {
        try
        {
            await _emailService.JobCompareEmailAsync(request);
            return Ok("Email sent successfully.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.ToString());
        }
    }
}

// Request model for email API endpoint
public class EmailRequest
{
    public string? Html { get; set; }        // Sprint table HTML content
    public string? Subject { get; set; }  // Sprint identifier for subject
}

