//using AForge.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toe;

namespace Rayman2FunBox {
    /// <summary>
    /// Transformation matrix storing position, rotation and scale. Also, an unknown vector4 and type.
    /// </summary>
    public class Matrix {
        public int offset;
        public UInt32 type;
        public Matrix4 m;
        public Matrix4? scaleMatrix;
        public Vector4 v;

        public Matrix(int offset, uint type, Matrix4 matrix, Vector4 vec)
        {
            this.offset = offset;
            this.type = type;
            this.m = matrix;
            this.v = vec;
        }

        public static Matrix Identity
        {
            get
            {
                return new Matrix(0, 0, Matrix4.Identity, new Vector4(1,1,1,1));
            }
        }

        public void SetScaleMatrix(Matrix4 scaleMatrix)
        {
            this.scaleMatrix = scaleMatrix;
        }

        public Vector3 GetPosition(bool convertAxes = false)
        {
            if (convertAxes) {
                return new Vector3(m.M14, m.M34, m.M24);
            } else {
                return new Vector3(m.M14, m.M24, m.M34);
            }
        }

        public Vector3 GetScale(bool convertAxes = false)
        {
            if (scaleMatrix.HasValue) {
                if (convertAxes) {
                    return new Vector3(scaleMatrix.Value.Column0.Length, scaleMatrix.Value.Column2.Length, scaleMatrix.Value.Column1.Length);
                } else {
                    return new Vector3(scaleMatrix.Value.Column0.Length, scaleMatrix.Value.Column1.Length, scaleMatrix.Value.Column2.Length);
                }
            } else {
                if (convertAxes) {
                    return new Vector3(m.Column0.Length, m.Column2.Length, m.Column1.Length);
                } else {
                    return new Vector3(m.Column0.Length, m.Column1.Length, m.Column2.Length);
                }
            }
        }


        public static Matrix operator *(Matrix x, Matrix y)
        {
            return new Matrix(x.offset, x.type, x.m * y.m, x.v);
        }

        static void SetRow(Matrix4 dest, int rowNum, Vector4 src)
        {
            if (rowNum == 0) {
                dest.M11 = src.X;
                dest.M12 = src.Y;
                dest.M13 = src.Z;
                dest.M14 = src.W;
            } else if (rowNum == 1) {
                dest.M21 = src.X;
                dest.M22 = src.Y;
                dest.M23 = src.Z;
                dest.M24 = src.W;
            } else if (rowNum == 2) {
                dest.M31 = src.X;
                dest.M32 = src.Y;
                dest.M33 = src.Z;
                dest.M34 = src.W;
            } else if (rowNum == 3) {
                dest.M41 = src.X;
                dest.M42 = src.Y;
                dest.M43 = src.Z;
                dest.M44 = src.W;
            } else {
                return;
            }
        }

        static void SetColumn(Matrix4 dest, int columnNum, Vector4 src)
        {
            if (columnNum == 0) {
                dest.M11 = src.X;
                dest.M21 = src.Y;
                dest.M31 = src.Z;
                dest.M41 = src.W;
            } else if (columnNum == 1) {
                dest.M12 = src.X;
                dest.M22 = src.Y;
                dest.M32 = src.Z;
                dest.M42 = src.W;
            } else if (columnNum == 2) {
                dest.M13 = src.X;
                dest.M23 = src.Y;
                dest.M33 = src.Z;
                dest.M43 = src.W;
            } else if (columnNum == 3) {
                dest.M14 = src.X;
                dest.M24 = src.Y;
                dest.M34 = src.Z;
                dest.M44 = src.W;
            } else {
                return;
            }
        }

        public static Matrix Invert(Matrix src)
        {
            Matrix dest = new Matrix(src.offset, src.type, new Matrix4(), src.v);
            
            SetRow(dest.m, 0, src.m.Row0);
            SetRow(dest.m, 1, src.m.Row1);
            SetRow(dest.m, 2, src.m.Row2);
            SetRow(dest.m, 3, src.m.Row3);

            SetColumn(dest.m, 0, src.m.Row0);
            SetColumn(dest.m, 1, src.m.Row1);
            SetColumn(dest.m, 2, src.m.Row2);

            dest.m.M14 = dest.m.M12 * -src.m.M24 + dest.m.M13 * -src.m.M34 + dest.m.M11 * -src.m.M14;
            dest.m.M24 = dest.m.M22 * -src.m.M24 + dest.m.M23 * -src.m.M34 + dest.m.M21 * -src.m.M14;
            dest.m.M34 = dest.m.M32 * -src.m.M24 + dest.m.M33 * -src.m.M34 + dest.m.M31 * -src.m.M14;

            SetRow(dest.m, 3, new Vector4(0f, 0f, 0f, 1f));
            return dest;
        }

        public Quaternion GetRotation()
        {
            float tr = m.M11 + m.M22 + m.M33;
            Quaternion q = new Quaternion();
            if (tr > 0) {
                float S = (float)Math.Sqrt(tr + 1.0f) * 2; // S=4*qW 
                q.W = 0.25f * S;
                q.X = (m.M32 - m.M23) / S;
                q.Y = (m.M13 - m.M31) / S;
                q.Z = (m.M21 - m.M12) / S;
            } else if ((m.M11 > m.M22) && (m.M11 > m.M33)) {
                float S = (float)Math.Sqrt(1.0f + m.M11 - m.M22 - m.M33) * 2; // S=4*qX 
                q.W = (m.M32 - m.M23) / S;
                q.X = 0.25f * S;
                q.Y = (m.M12 + m.M21) / S;
                q.Z = (m.M13 + m.M31) / S;
            } else if (m.M22 > m.M33) {
                float S = (float)Math.Sqrt(1.0f + m.M22 - m.M11 - m.M33) * 2; // S=4*qY
                q.W = (m.M13 - m.M31) / S;
                q.X = (m.M12 + m.M21) / S;
                q.Y = 0.25f * S;
                q.Z = (m.M23 + m.M32) / S;
            } else {
                float S = (float)Math.Sqrt(1.0f + m.M33 - m.M11 - m.M22) * 2; // S=4*qZ
                q.W = (m.M21 - m.M12) / S;
                q.X = (m.M13 + m.M31) / S;
                q.Y = (m.M23 + m.M32) / S;
                q.Z = 0.25f * S;
            }

            return q;
        }

        public static Matrix Read(int processHandle, int offset)
        {
            byte[] buffer = new byte[22 * 4]; // 22 dwords/floats
            int bytesRead = 0;
            Memory.ReadProcessMemory(processHandle, offset, buffer, buffer.Length, ref bytesRead);

            int off = 0;
            UInt32 type = BitConverter.ToUInt32(buffer, (off++) * 4); // 0x02: always at the start of a transformation matrix
            Matrix mat = new Matrix(offset, type, new Matrix4(), Vector4.One);
            
            Vector3 pos = Vector3.Zero;

            pos = new Vector3(BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4));

            mat.m.Column0 = new Vector4(BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), 0f);
            mat.m.Column1 = new Vector4(BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), 0f);
            mat.m.Column2 = new Vector4(BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), 0f);

            Matrix4 sclMatrix = new Matrix4();

            sclMatrix.Column0 = new Vector4(BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), 0f);
            sclMatrix.Column1 = new Vector4(BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), 0f);
            sclMatrix.Column2 = new Vector4(BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), BitConverter.ToSingle(buffer, (off++) * 4), 0f);
 

            mat.m.Column3 = new Vector4(pos.X, pos.Y, pos.Z, 1f);
            sclMatrix.Column3 = new Vector4(0, 0, 0, 1f);
            mat.SetScaleMatrix(sclMatrix);

            return mat;
        }

        public static float AngleDifference(float a, float b)
        {
            float diff = b - a;
            while(diff>Math.PI) {
                diff -= (float)Math.PI * 2;
            }
            while(diff<-Math.PI) {
                diff += (float)Math.PI * 2;
            }

            return diff;
        }

        public static Vector3 QuaternionToEuler(Quaternion q)
        {
            var rotation = q;
            double q0 = rotation.W;
            double q1 = rotation.Y;
            double q2 = rotation.X;
            double q3 = rotation.Z;

            Vector3 radAngles = new Vector3();
            radAngles.Y = (float)Math.Atan2(2 * (q0 * q1 + q2 * q3), 1 - 2 * (Math.Pow(q1, 2) + Math.Pow(q2, 2)));
            float xFactor = (float)(2 * (q0 * q2 - q3 * q1));
            radAngles.X = (float)Math.Asin(Math.Min(Math.Max(xFactor,-1),1));
            radAngles.Z = (float)Math.Atan2(2 * (q0 * q3 + q1 * q2), 1 - 2 * (Math.Pow(q2, 2) + Math.Pow(q3, 2)));

            return radAngles;
        }

        // For writing
        public void SetTRS(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            m = Matrix4.CreateTranslation(pos) * Matrix4.CreateScale(scale) * Matrix4.CreateFromQuaternion(rot);
        }

        public void Write(int processHandle, int offset)
        {
            byte[] bytes = new byte[16 * 4]; // 16 dwords/floats
            int off = 0;
            BitConverter.GetBytes(type).CopyTo(bytes, (off++) * 4);

            Vector4 pos = m.Column3;
            BitConverter.GetBytes(pos.X).CopyTo(bytes, (off++) * 4);
            BitConverter.GetBytes(pos.Y).CopyTo(bytes, (off++) * 4);
            BitConverter.GetBytes(pos.Z).CopyTo(bytes, (off++) * 4);
            if (type == 4) {
                //off = offset + 36;
            }
            for (int j = 0; j < 3; j++) {

                Vector4 col = new Vector4();
                if (j == 0) {
                    col = m.Column0;
                } else if (j == 1) {
                    col = m.Column1;
                } else if(j == 2) {
                    col = m.Column2;
                } else if (j == 3) {
                    col = m.Column3;
                } else {
                    break;
                }

                BitConverter.GetBytes(col.X).CopyTo(bytes, (off++) * 4);
                BitConverter.GetBytes(col.Y).CopyTo(bytes, (off++) * 4);
                BitConverter.GetBytes(col.Z).CopyTo(bytes, (off++) * 4);
            }

            int bytesWritten = 0;
            Memory.WriteProcessMemory(processHandle, offset, bytes, bytes.Length, ref bytesWritten);
        }
    }
}
