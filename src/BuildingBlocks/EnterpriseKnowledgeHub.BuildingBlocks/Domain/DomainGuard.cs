namespace EnterpriseKnowledgeHub.BuildingBlocks.Domain
{
    public static class DomainGuard
    {
        public static string Required(
            string value,
            string propertyName,
            int maxLength = 256)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException($"{propertyName} cannot be empty.");
            }

            return MaxLength(value, propertyName, maxLength);
        }

        public static string MaxLength(
            string value,
            string propertyName,
            int maxLength)
        {
            if (value.Length > maxLength)
            {
                throw new DomainException($"{propertyName} cannot exceed {maxLength} characters.");
            }

            return value;
        }
    }
}
