namespace Cs2MarketApi.Application.Exceptions;

public class SteamNotFoundException(string message) : Exception(message);

public class InventoryPrivateException(string steamId)
    : Exception($"Inventory of {steamId} is private or unavailable.");

public class ExternalServiceException(string message, Exception? inner = null) : Exception(message, inner);
public class GamesPrivateException(string steamId)
    : Exception($"Game library of {steamId} is private. Set 'Game details' to public in Steam privacy settings.");