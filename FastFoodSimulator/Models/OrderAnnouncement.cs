namespace FastFoodSimulator.Models;

public sealed record OrderAnnouncement(int OrderNumber, int CustomerNumber, TimeSpan AnnouncedAt);
