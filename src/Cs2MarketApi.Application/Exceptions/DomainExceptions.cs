namespace Cs2MarketApi.Application.Exceptions;

public class SteamNotFoundException(string message) : Exception(message);

public class InventoryPrivateException(string steamId)
    : Exception($"Inventory of {steamId} is private or unavailable.");

public class ExternalServiceException(string message, Exception? inner = null) : Exception(message, inner);
