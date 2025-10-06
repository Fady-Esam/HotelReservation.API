﻿namespace HotelReservation.API.Common.Settings
{
    public class JwtSetting
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public double ExpirationInMinutes { get; set; }
    }
}
