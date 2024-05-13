namespace AuthenticationProject.API.Models
{
    public class InviteUserModel
    {
        public string Email { get; set; } = default!;
        public int Role { get; set; }
    }
}
