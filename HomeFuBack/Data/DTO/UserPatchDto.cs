namespace HomeFuBack.Data.DTO
{
    public class UserPatchDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }

        // Email и Password здесь не являются [Required]
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
