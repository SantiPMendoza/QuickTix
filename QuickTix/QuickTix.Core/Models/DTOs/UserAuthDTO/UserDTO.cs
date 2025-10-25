﻿namespace QuickTix.Core.Models.DTOs.UserAuthDTO
{
    public class UserDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
