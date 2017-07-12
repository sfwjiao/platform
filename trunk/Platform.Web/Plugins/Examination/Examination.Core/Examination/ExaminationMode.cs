using System;

namespace Examination.Examination
{
    [Flags]
    public enum ExaminationMode
    {
        /// <summary>
        /// 在线考试
        /// </summary>
        OnlineTesting = 1 << 0,
        /// <summary>
        /// 现场打分
        /// </summary>
        LiveScoring = 1 << 1,
    }
}