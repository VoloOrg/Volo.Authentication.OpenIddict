namespace AuthenticationProject.API.Models
{
    public class Role
    {
        public int Id { get; init; }
        public string Name { get; init; } = default!;

        private Role()
        {

        }

        public static readonly Role Admin = new Role() { Id = 1, Name = nameof(Admin) };
        public static readonly Role General = new Role() { Id = 2, Name = nameof(General) };
        public static readonly Role Special = new Role() { Id = 3, Name = nameof(Special) };

        public static readonly List<Role> AllRoles = [Admin, General, Special];
    }
}
