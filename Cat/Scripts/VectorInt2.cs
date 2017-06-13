using System;
using UnityEngine;

namespace Cat.Common {

	public struct VectorInt2 {
		public const float kEpsilon = 1E-05f;
		public int x;
		public int y;
		public int this[int index] {
			get {
                switch (index) {
                    case 0: return x;
                    case 1: return y;
                    default: throw new IndexOutOfRangeException("Invalid VectorInt2 index!");
                }
			}
			set {
                switch (index) {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    default: throw new IndexOutOfRangeException("Invalid VectorInt2 index!");
                }
			}
		}
		public float magnitude { get { return Mathf.Sqrt(sqrMagnitude); } }
		public int sqrMagnitude {
			get { return x*x + y*y; }
		}
		public static VectorInt2 zero {
			get { return new VectorInt2(0, 0); }
		}
		public static VectorInt2 one {
			get { return new VectorInt2(1, 1); }
		}
		public static VectorInt2 up {
			get { return new VectorInt2(0, 1); }
		}
		public static VectorInt2 right {
			get { return new VectorInt2(1, 0); }
		}
        
		public VectorInt2(int x, int y) {
			this.x = x;
			this.y = y;
		}
		public void Set(int x, int y) {
			this.x = x;
			this.y = y;
		}
		public static Vector2 Scale(VectorInt2 left, VectorInt2 right) {
			return new Vector2(left.x * right.x, left.y * right.y);
		}
		public void Scale(VectorInt2 scale) {
			x *= scale.x;
			y *= scale.y;
		}
		public override string ToString() {
			return String.Format("({0}, {1})", x, y);
		}
		public string ToString(string format) {
			return String.Format("({0}, {1})", x.ToString(format), y.ToString(format));
		}
		public override int GetHashCode() {
			return x.GetHashCode() ^ y.GetHashCode() << 2;
		}
		public override bool Equals(object other) {
			if (!(other is VectorInt2)) {
				return false;
			}
			VectorInt2 otherv = (VectorInt2)other;
			return (x == otherv.x) && (y == otherv.y);
		}
		public static int Dot(VectorInt2 left, VectorInt2 right) {
			return left.x * right.x + left.y * right.y;
		}
		public static float Distance(VectorInt2 a, VectorInt2 b) {
			return (a - b).magnitude;
		}
		public static VectorInt2 Min(VectorInt2 left, VectorInt2 right) {
			return new VectorInt2(Mathf.Min(left.x, right.x), Mathf.Min(left.y, right.y));
		}
		public static VectorInt2 Max(VectorInt2 left, VectorInt2 right) {
			return new VectorInt2(Mathf.Max(left.x, right.x), Mathf.Max(left.y, right.y));
		}
		public static VectorInt2 operator +(VectorInt2 left, VectorInt2 right) {
			return new VectorInt2(left.x + right.x, left.y + right.y);
		}
		public static VectorInt2 operator -(VectorInt2 left, VectorInt2 right) {
			return new VectorInt2(left.x - right.x, left.y - right.y);
		}
		public static VectorInt2 operator -(VectorInt2 left) {
			return new VectorInt2(-left.x, -left.y);
		}
		public static VectorInt2 operator *(VectorInt2 left, int d) {
			return new VectorInt2(left.x * d, left.y * d);
		}
		public static VectorInt2 operator *(int d, VectorInt2 left) {
			return new VectorInt2(left.x * d, left.y * d);
		}
        	public static Vector2 operator *(VectorInt2 left, float d) {
            return (Vector2)left * d;
        }
		public static Vector2 operator *(float d, VectorInt2 left) {
			return (Vector2)left * d;
		}
		public static VectorInt2 operator /(VectorInt2 left, int d) {
			return new VectorInt2(left.x / d, left.y / d);
		}
		public static Vector2 operator /(VectorInt2 left, float d) {
			return (Vector2)left / d;
		}
		public static bool operator ==(VectorInt2 left, VectorInt2 right) {
			return Vector2.SqrMagnitude(left - right) == 0;
		}
		public static bool operator !=(VectorInt2 left, VectorInt2 right) {
			return Vector2.SqrMagnitude(left - right) != 0;
		}
		public static implicit operator Vector2(VectorInt2 v) {
			return new Vector2(v.x, v.y);
		}
		public static implicit operator Vector3(VectorInt2 v) {
			return (Vector2)v;
		}
		public static implicit operator Vector4(VectorInt2 v) {
			return (Vector2)v;
		}
		public static explicit operator VectorInt2(Vector2 v) {
			return new VectorInt2((int)v.x, (int)v.y);
		}
	}
    
}
