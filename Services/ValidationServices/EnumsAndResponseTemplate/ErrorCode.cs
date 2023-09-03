namespace VergilBot.Services.ValidationServices.EnumsAndResponseTemplate;

public enum ErrorCode
{
    Success = 0,
    InvalidHeight = 1,
    InvalidWidth = 2,
    InvalidHeightOrWidth = 3,
    NotFound = 4
}