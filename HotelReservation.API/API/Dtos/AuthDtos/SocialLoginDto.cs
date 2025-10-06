﻿using System.ComponentModel.DataAnnotations;

namespace HotelReservation.API.API.Dtos.AuthDtos
{
    public class SocialLoginDto
    {
        public string Token { get; set; } = string.Empty; // ID token (Google) or Access token (Facebook) or Identity token (Apple)
    }
}
