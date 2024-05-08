namespace AuthenticationProject.API.Models
{
    public class CurrentUserModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
