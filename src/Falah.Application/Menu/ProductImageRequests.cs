namespace Falah.Application.Menu;

/// <summary>Uploading a product photo. The bytes arrive base64-encoded inside the JSON body rather
/// than as multipart form data: every other write in this system is a JSON command carrying an
/// ActorUserId, and a menu photo is not worth a second request shape and a second authorization
/// path. Menu images are small by policy (see ProductImageRules), so the encoding overhead is
/// measured in kilobytes.</summary>
public sealed record SetProductImageRequest(
    string ActorUserId,
    string ProductId,
    string ContentType,
    string Base64Bytes);

public sealed record SetProductImageResult(string ProductId, string Version);

public sealed record RemoveProductImageRequest(string ActorUserId, string ProductId);
