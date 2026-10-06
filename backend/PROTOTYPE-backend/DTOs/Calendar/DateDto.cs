namespace PROTOTYPE_backend.DTOs.Calendar
{
    public record DateDto(
            Guid ProjectId,
            Guid DateId,
            DateTime timeStamp,
            string Title,
            string Description
        );
}
