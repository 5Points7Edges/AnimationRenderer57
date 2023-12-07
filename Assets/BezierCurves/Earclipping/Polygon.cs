
using UnityEngine;
using System.Collections.Generic;

namespace GenericShape
{
    public struct Polygon
    {
        public Vector2[] points;

        public Polygon(Vector2[] points)
        {
            this.points = new Vector2[points.Length];
            points.CopyTo(this.points, 0);

        }

        /// <summary>
        /// 三角形化
        /// </summary>
        /// <returns></returns>
        public Triangle[] Triangulate()
        {
            if (points.Length < 3)
            {
                return new Triangle[0];
            }
            else
            {
                // 节点数量
                int count = points.Length;
                // 确定方向
                bool isClockWise = IsClockWise();
                // 初始化节点
                PointNode curNode = GenPointNote();
                // 三角形数量
                int triangleCount = count - 2;
                // 获取三角形
                List<Triangle> triangles = new List<Triangle>();
                AngleType angleType;
                while (triangles.Count < triangleCount)
                {
                    // 获取耳点
                    int i = 0, maxI = count - 1;
                    for (; i <= maxI; i++)
                    {
                        angleType = GetAngleType(curNode, isClockWise);
                        if (angleType == AngleType.StraightAngle)
                        {
                            // 等于180，不可能为耳点
                            // 移除当前点，三角形数量少一个
                            curNode = RemovePoint(curNode);
                            count--;
                            triangleCount--;
                        }
                        else if (angleType == AngleType.ReflexAngle)
                        {
                            // 大于180，不可能为耳点
                            curNode = curNode.NextNode;
                        }
                        else if (IsInsideOtherPoint(curNode, count))
                        {
                            //包含其他点，不可能为耳点
                            curNode = curNode.NextNode;
                        }
                        else
                        {
                            // 当前点就是ear，添加三角形,移除当前节点
                            triangles.Add(GenTriangle(curNode));
                            curNode = RemovePoint(curNode);
                            count--;
                            break;
                        }
                    }
                    // DebugDraw(curNode, count, triangles);
                    // 还需要分割耳点,但找不到ear
                    if (triangles.Count < triangleCount && i > maxI)
                    {
                        Debug.Log("找不到ear");
                        triangles.Clear();
                        break;
                    }
                }
                return triangles.ToArray();
            }
        }

        /// <summary>
        /// 生成点节点
        /// </summary>
        private PointNode GenPointNote()
        {
            // 创建第一个节点
            PointNode firstNode = new PointNode(points[0]);
            // 创建后续节点
            PointNode now = firstNode, previous;
            // Vector2[] points
            for (int i = 1; i < points.Length; i++)
            {
                previous = now;
                now = new PointNode(points[i]);
                // 关联
                now.PreviousNode = previous;
                previous.NextNode = now;
            }
            // 关联头尾
            firstNode.PreviousNode = now;
            now.NextNode = firstNode;
            return firstNode;
        }

        /// <summary>
        /// 当前的点方向是否为顺时针
        /// </summary>
        /// <returns></returns>
        private bool IsClockWise()
        {
            // 通过计算叉乘来确定方向
            float sum = 0f;
            double count = points.Length;
            Vector3 va, vb;
            for (int i = 0; i < points.Length; i++)
            {
                va = points[i];
                vb = (i == count - 1) ? points[0] : points[i + 1];
                sum += va.x * vb.y - va.y * vb.x;
            }
            return sum < 0;
        }

        /// <summary>
        /// 当前点组成的三角形，是否包含其他点
        /// </summary>
        private bool IsInsideOtherPoint(PointNode node, int count)
        {
            bool flag = false;
            int checkCount = count - 3;
            //now 第一个开始校验其实是node.NextNode.NextNode
            PointNode now = node.NextNode;
            for (int i = 0; i < checkCount; i++)
            {
                now = now.NextNode;
                if (IsInside(node, now.Position))
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }

        /// <summary>
        /// 判断角的类型
        /// </summary>
        private AngleType GetAngleType(PointNode node, bool isClockWise)
        {
            // 角度是否小于180
            // oa & ob 之间的夹角，（右手法则）
            // 逆时针顺序是相反的
            Vector2 o = node.Position;
            Vector2 a = node.PreviousNode.Position;
            Vector2 b = node.NextNode.Position;
            float f = (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            bool flag = isClockWise ? f > 0 : f < 0;
            if (f == 0)
            {
                return AngleType.StraightAngle;
            }
            else if (flag)
            {
                return AngleType.InferiorAngle;
            }
            else
            {
                return AngleType.ReflexAngle;
            }
        }

        /// <summary>
        /// 判断角的类型,oa & ob 之间的夹角，（右手法则）
        /// </summary>
        // private AngleType GetAngleType(Vector2 o, Vector2 a, Vector2 b, bool isClockWise)
        // {
        //     float f = (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        //     bool flag = isClockWise ? f > 0 : f < 0;
        //     if (f == 0)
        //     {
        //         return AngleType.StraightAngle;
        //     }
        //     else if (flag)
        //     {
        //         return AngleType.InferiorAngle;
        //     }
        //     else
        //     {
        //         return AngleType.ReflexAngle;
        //     }
        // }

        /// <summary>
        /// p点是否在点和其左右两个点组成的三角形内,或ca,cb边上
        /// </summary>
        private bool IsInside(PointNode node, Vector2 p)
        {
            // p点是否在abc三角形内
            Vector2 a = node.PreviousNode.Position;
            Vector2 b = node.NextNode.Position;
            Vector2 c = node.Position;
            var c1 = (b.x - a.x) * (p.y - b.y) - (b.y - a.y) * (p.x - b.x);
            var c2 = (c.x - b.x) * (p.y - c.y) - (c.y - b.y) * (p.x - c.x);
            var c3 = (a.x - c.x) * (p.y - a.y) - (a.y - c.y) * (p.x - a.x);
            return
                (c1 > 0f && c2 >= 0f && c3 >= 0f) ||
                (c1 < 0f && c2 <= 0f && c3 <= 0f);
        }

        // // / <summary>
        // // / p点是否在点和其左右两个点组成的三角形内,或ca,cb边上
        // // / </summary>
        // private bool IsInside(Vector2 c, Vector2 a, Vector2 b, Vector2 p)
        // {
        //     // p点是否在abc三角形内
        //     var c1 = (b.x - a.x) * (p.y - b.y) - (b.y - a.y) * (p.x - b.x);
        //     var c2 = (c.x - b.x) * (p.y - c.y) - (c.y - b.y) * (p.x - c.x);
        //     var c3 = (a.x - c.x) * (p.y - a.y) - (a.y - c.y) * (p.x - a.x);
        //     return
        //         // (c1 > 0f && c2 > 0f && c3 > 0f) ||
        //         // (c1 < 0f && c2 < 0f && c3 < 0f);
        //         (c1 > 0f && c2 >= 0f && c3 >= 0f) ||
        //         (c1 < 0f && c2 <= 0f && c3 <= 0f);
        // }

        /// <summary>
        /// 删除当前节点，把前后节点关联,返回下一个节点
        /// </summary>
        private PointNode RemovePoint(PointNode node)
        {
            PointNode previous = node.PreviousNode;
            PointNode next = node.NextNode;
            previous.NextNode = next;
            next.PreviousNode = previous;
            return next;
        }

        /// <summary>
        /// 生成点和其左右两个点组成的三角形内
        /// </summary>
        /// <returns></returns>
        private Triangle GenTriangle(PointNode node)
        {
            Triangle triangle = new Triangle();
            triangle.a = node.Position;
            triangle.b = node.PreviousNode.Position;
            triangle.c = node.NextNode.Position;
            return triangle;
        }

        // private Vector2 offset;
        // private void DebugDraw(PointNode curNode, int count, List<Triangle> triangles)
        // {
        //     offset += new Vector2(400, 0);
        //     // 画多边形
        //     List<Vector2> points = new List<Vector2>(count);
        //     for (int i = 0; i < count; i++)
        //     {
        //         points.Add(curNode.Position + offset);
        //         curNode = curNode.NextNode;
        //     }
        //     Utils.DebugF.DrawPolygon(points, Color.red, 1000);
        //     for (int i = 0; i < points.Count; i++)
        //     {
        //         Utils.DebugF.DrawCricle(points[i], 8, 0.1f, Color.red, 1000);
        //     }
        //     // 画三角形
        //     if (triangles != null)
        //     {
        //         for (int i = 0; i < triangles.Count; i++)
        //         {
        //             Utils.DebugF.DrawTriangle(
        //                 triangles[i].a + offset + new Vector2(-5, 0),
        //                 triangles[i].b + offset + new Vector2(-5, 0),
        //                 triangles[i].c + offset + new Vector2(-5, 0),
        //                 Color.blue, 1000);
        //         }
        //     }
        // }

    }
}
