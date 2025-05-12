namespace HomeFuBack.Helpers
{
    public class UniqueFileName
    {
        public static string GetUniqueFileName(string fileName)
        {
            fileName = Path.GetFileName(fileName);
            return $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        }
    }
}
