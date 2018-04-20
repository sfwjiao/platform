namespace Platform.Users.Dto
{
    public class UpdatePwdInput
    {
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 原密码
        /// </summary>
        public string OldPassword { get; set; }
    }
}
