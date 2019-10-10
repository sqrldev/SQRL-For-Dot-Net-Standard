namespace SqrlForNet
{
    public class OtherAuthenticationPath
    {
        /// <summary>
        /// The path to allow SQRL requests over
        /// </summary>
        public string Path;

        /// <summary>
        /// Is this path to be authenticated separately with other paths (a new user id is created for each path this is true for)
        /// </summary>
        public bool AuthenticateSeparately;

    }
}
