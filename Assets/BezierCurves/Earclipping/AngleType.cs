namespace GenericShape
{
    enum AngleType
    {
        /// <summary>
        /// 平角 = 180
        /// </summary>
        StraightAngle = 0,

        /// <summary>
        /// 优角 >180
        /// </summary>
        ReflexAngle = 1,

        /// <summary>
        /// 劣角 <180
        /// </summary>
        InferiorAngle = 2,
    }
}