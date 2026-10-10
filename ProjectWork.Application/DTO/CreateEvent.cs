using System.ComponentModel.DataAnnotations;

namespace ProjectWork.Application.DTO
{
    public class CreateEvent
    {
        [Required(ErrorMessage = "Название обязательно для заполнения")]
        public required string Title { get; set; }

        public string? Description { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        [Required(ErrorMessage = "Количество мест обязательно для заполнения")]
        public int? TotalSeats { get; set; }
    }
}
