namespace RimerApi.Application.DTOs.Ticket;

public class DepartmentStatsDto
{
    public int Total { get; set; }
    public int NewTickets { get; set; }
    public int Reviewing { get; set; }
    public int MyAssigned { get; set; }
    public int Critical { get; set; }
    public int Delayed45 { get; set; }
}
