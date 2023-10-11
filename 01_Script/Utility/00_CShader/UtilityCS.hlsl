#ifndef UTILITYCS_HLSL
#define UTILITYCS_HLSL

#define v3c0 _m00_m10_m20
#define v3c1 _m01_m11_m21
#define v3c2 _m02_m12_m22
#define v3c3 _m03_m13_m23

#define v4c0 _m00_m10_m20_m30
#define v4c1 _m01_m11_m21_m31
#define v4c2 _m02_m12_m22_m32
#define v4c3 _m03_m13_m23_m33

#define v3r0 _m00_m01_m02
#define v3r1 _m10_m11_m12
#define v3r2 _m20_m21_m22
#define v3r3 _m30_m31_m32

#define v4r0 _m00_m01_m02_m03
#define v4r1 _m10_m11_m12_m13
#define v4r2 _m20_m21_m22_m23
#define v4r3 _m30_m31_m32_m33

#define f3zero float3(0.0f, 0.0f, 0.0f);
#define f3x3I float3x3(1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
#define f3x3Zero float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
#define f4x4I float4x4(1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f)
#define f4x4Zero float4x4(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
#define qI float4(0.0f, 0.0f, 0.0f, 1.0f);

#define FLOAT_MIN  -3.40282347e+38f;
#define FLOAT_MAX  +3.40282347e+38f;

struct Triangle
{
    static bool TestLineSegmentToTriangle(float3x2 lineSeg, float3x3 tri, out float3 result)
    {
        float3 r = float3(0.0f, 0.0f, 0.0f);
        float3x3 M = float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

        M.v3c0 = lineSeg.v3c0 - lineSeg.v3c1;
        M.v3c1 = tri.v3c1 - tri.v3c0;
        M.v3c2 = tri.v3c2 - tri.v3c0;
        float3 v = lineSeg.v3c0 - tri.v3c0;

		//optimized
        float3 area1 = cross(M.v3c1, M.v3c2);
        float3 area2 = cross(M.v3c0, v);
        float det = dot(M.v3c0, area1);
        r.x = dot(v, area1) / det;
        r.y = dot(M.v3c2, area2) / det;
        r.z = dot(M.v3c1, -area2) / det;

        result = r;
        if ((r.x >= 0.0f && r.x <= 1.0f) &&
			(r.y >= 0.0f && r.y <= 1.0f) &&
			(r.z >= 0.0f && r.z <= 1.0f) &&
			(r.y + r.z >= 0.0f && r.y + r.z <= 1.0f))
        {
            return true;
        }

        return false;
    }
    
    static bool TestLineSegmentToTriangle_Picking(float3 lineSeg[2], float3 tri[3], out float3 result)
    {
        float3 r = float3(0.0f, 0.0f, 0.0f);
        float3x3 M = float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                    
        M.v3c0 = lineSeg[0] - lineSeg[1];
        M.v3c1 = tri[1] - tri[0];
        M.v3c2 = tri[2] - tri[0];
        float3 v = lineSeg[0] - tri[0];
        
        bool isFront = false;
        float3 n0 = normalize(M.v3c0);
        float3 n1 = normalize(cross(M.v3c1, M.v3c2));
    #if defined(SHADER_API_D3D11)
        isFront = dot(n0, n1) > 0.0f ? true : false;
    #elif defined(SHADER_API_GLCORE)
        isFront = dot(n0, n1) > 0.0f ? true : false;
    #elif defined(SHADER_API_VULKAN)
        isFront = dot(n0, n1) > 0.0f ? true : false;
    #endif
        
        if(isFront)
        {
            //optimized
            float3 area1 = cross(M.v3c1, M.v3c2);
            float3 area2 = cross(M.v3c0, v);
            float det = dot(M.v3c0, area1);
            r.x = dot(v, area1) / det;
            r.y = dot(M.v3c2, area2) / det;
            r.z = dot(M.v3c1, -area2) / det;
            
            if ((r.x >= 0.0f && r.x <= 1.0f) &&
			(r.y >= 0.0f && r.y <= 1.0f) &&
			(r.z >= 0.0f && r.z <= 1.0f) &&
			(r.y + r.z >= 0.0f && r.y + r.z <= 1.0f))
            {
                return true;
            }
        }		
        result = r;
        
        return false;
    }
    
    static bool TestLineSegmentToTriangle_Picking_Compute(float3 lineSeg[2], float3 tri[3], out float4x4 result)
    {
        float3 r = float3(0.0f, 0.0f, 0.0f);
        float3x3 M = float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                    
        M.v3c0 = lineSeg[0] - lineSeg[1];
        M.v3c1 = tri[1] - tri[0];
        M.v3c2 = tri[2] - tri[0];
        float3 v = lineSeg[0] - tri[0];
        
        bool isFront = false;
        float3 n0 = normalize(M.v3c0);
        float3 n1 = normalize(cross(M.v3c1, M.v3c2));
#if defined(SHADER_API_D3D11)
        isFront = dot(n0, n1) > 0.0f ? true : false;
#elif defined(SHADER_API_GLCORE)
        isFront = dot(n0, n1) > 0.0f ? true : false;
#elif defined(SHADER_API_VULKAN)
        isFront = dot(n0, n1) > 0.0f ? true : false;
#endif
        
        result = f4x4Zero;
               
        if (isFront)
        {
            //optimized
            float3 area1 = cross(M.v3c1, M.v3c2);
            float3 area2 = cross(M.v3c0, v);
            float det = dot(M.v3c0, area1);
            r.x = dot(v, area1) / det;
            r.y = dot(M.v3c2, area2) / det;
            r.z = dot(M.v3c1, -area2) / det;
            
            if ((r.x >= 0.0f && r.x <= 1.0f) &&
			(r.y >= 0.0f && r.y <= 1.0f) &&
			(r.z >= 0.0f && r.z <= 1.0f) &&
			(r.y + r.z >= 0.0f && r.y + r.z <= 1.0f))
            {
                result.v3c0 = tri[0];
                result.v3c1 = tri[1];
                result.v3c2 = tri[2];
                result.v3c3 = n1;
                result.v3r3 = r;
                result._m33 = 1.0f;
                
                return true;
            }
        }        
        
        return false;
    }

    static bool TestTriangleToTriangleOutPlane(float3x3 triA, float3x3 triB)
    {
        float3 result = float3(0.0f, 0.0f, 0.0f);
        float3x2 lineSeg = float3x2(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

		//lineSegA_triB
        lineSeg.v3c0 = triA.v3c0;
        lineSeg.v3c1 = triA.v3c1;
        if (Triangle::TestLineSegmentToTriangle(lineSeg, triB, result))
        {
            return true;
        }

        lineSeg.v3c0 = triA.v3c1;
        lineSeg.v3c1 = triA.v3c2;
        if (Triangle::TestLineSegmentToTriangle(lineSeg, triB, result))
        {
            return true;
        }

        lineSeg.v3c0 = triA.v3c2;
        lineSeg.v3c1 = triA.v3c0;
        if (Triangle::TestLineSegmentToTriangle(lineSeg, triB, result))
        {
            return true;
        }

		//triA_lineSegB
        lineSeg.v3c0 = triB.v3c0;
        lineSeg.v3c1 = triB.v3c1;
        if (Triangle::TestLineSegmentToTriangle(lineSeg, triA, result))
        {
            return true;
        }

        lineSeg.v3c0 = triB.v3c1;
        lineSeg.v3c1 = triB.v3c2;
        if (Triangle::TestLineSegmentToTriangle(lineSeg, triA, result))
        {
            return true;
        }

        lineSeg.v3c0 = triB.v3c2;
        lineSeg.v3c1 = triB.v3c0;
        if (Triangle::TestLineSegmentToTriangle(lineSeg, triA, result))
        {
            return true;
        }

        return false;
    }

    static bool TestTriangleToTriangleSimple(float3x3 triA, float3x3 triB, float4x4 AfromB)
    {
        triB.v3c0 = mul(AfromB, float4(triB.v3c0, 1.0f)).xyz;
        triB.v3c1 = mul(AfromB, float4(triB.v3c1, 1.0f)).xyz;
        triB.v3c2 = mul(AfromB, float4(triB.v3c2, 1.0f)).xyz;

        if (Triangle::TestTriangleToTriangleOutPlane(triA, triB))
        {
            return true;
        }

        return false;
    }
};

struct Plane
{
    float3x2 plane;

    static bool TestPointToPlaneInOut(float3 pos, float3x2 plane)
    {
        if (dot(plane.v3c0, pos - plane.v3c1) <= 0.0f)
        {
            return true;
        }

        return false;
    }
};

struct HHCollider
{
    static bool TestPointToPlaneInOut(float3 pos, float3x2 plane)
    {
        if (dot(plane.v3c0, pos - plane.v3c1) <= 0.0f)
        {
            return true;
        }

        return false;
    }
	
    static bool TestCentersToBoundingPlanes(
		float3 centerA, float3 centerB, float4x4 AfromB,
		StructuredBuffer<Plane> planesA, StructuredBuffer<Plane> planesB, float4x4 AfromBnormal)
    {
        int a = 0;
        int b = 0;
        int numPlaneA = 6;
        int numPlaneB = 6;

        float3 center = mul(AfromB, float4(centerB, 1.0f)).xyz;
        for (int i = 0; i < numPlaneA; i++)
        {
            if (HHCollider::TestPointToPlaneInOut(center, planesA[i].plane))
            {
                a = a + 1;
            }
            else
            {
                break;
            }

        }

        for (int j = 0; j < numPlaneB; j++)
        {
            float3x2 plane;
            plane.v3c0 = mul(AfromBnormal, float4(planesB[j].plane.v3c0, 0.0f)).xyz;
            plane.v3c1 = mul(AfromB, float4(planesB[j].plane.v3c1, 1.0f)).xyz;
            if (HHCollider::TestPointToPlaneInOut(centerA, plane))
            {
                b = b + 1;
            }
            else
            {
                break;
            }
        }

        if ((a == numPlaneA) || (b == numPlaneB))
        {
            return true;
        }

        return false;
    }
			
    static bool TestMeshToMesh(
		Buffer<float3> posA, Buffer<float3> posB,
		Buffer<int> indiA, Buffer<int> indiB,
		float4x4 AfromB)
    {
        float3x3 triA = float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        float3x3 triB = float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
		 
        for (int i = 0; i < 36; i = i + 3)
        {
            for (int j = 0; j < 36; j = j + 3)
            {
                triA.v3c0 = posA[indiA[i + 0]];
                triA.v3c1 = posA[indiA[i + 1]];
                triA.v3c2 = posA[indiA[i + 2]];

                triB.v3c0 = posB[indiB[j + 0]];
                triB.v3c1 = posB[indiB[j + 1]];
                triB.v3c2 = posB[indiB[j + 2]];

                if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool TestHCtoHC(
		Buffer<float3> posA, Buffer<float3> posB,
		Buffer<int> indexA, Buffer<int> indexB,
		float3 centerA, float3 centerB,
		StructuredBuffer<Plane> planeA, StructuredBuffer<Plane> planeB,
		float4x4 AfromB, float4x4 AfromBnormal)
    {
        if (HHCollider::TestCentersToBoundingPlanes(
			centerA, centerB, AfromB,
			planeA, planeB, AfromBnormal))
        {
            return true;
        }
        else if (HHCollider::TestMeshToMesh(
			posA, posB,
			indexA, indexB, AfromB))
        {
            return true;
        }

        return false;
    }
	
	/*
	static bool TestMeshToMesh1(
		Buffer<float3> posA, Buffer<float3> posB,
		Buffer<int> indiA, Buffer<int> indiB,
		float4x4 AfromB
	)
	{
		float3x3 triA = f3x3Zero;

		triA.v3c0 = posA[indiA[0]];
		triA.v3c1 = posA[indiA[1]];
		triA.v3c2 = posA[indiA[2]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[3]];
		triA.v3c1 = posA[indiA[4]];
		triA.v3c2 = posA[indiA[5]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[6]];
		triA.v3c1 = posA[indiA[7]];
		triA.v3c2 = posA[indiA[8]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[9]];
		triA.v3c1 = posA[indiA[10]];
		triA.v3c2 = posA[indiA[11]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[12]];
		triA.v3c1 = posA[indiA[13]];
		triA.v3c2 = posA[indiA[14]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[15]];
		triA.v3c1 = posA[indiA[16]];
		triA.v3c2 = posA[indiA[17]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[18]];
		triA.v3c1 = posA[indiA[19]];
		triA.v3c2 = posA[indiA[20]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[21]];
		triA.v3c1 = posA[indiA[22]];
		triA.v3c2 = posA[indiA[23]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[24]];
		triA.v3c1 = posA[indiA[25]];
		triA.v3c2 = posA[indiA[26]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[27]];
		triA.v3c1 = posA[indiA[28]];
		triA.v3c2 = posA[indiA[29]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[30]];
		triA.v3c1 = posA[indiA[31]];
		triA.v3c2 = posA[indiA[32]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		triA.v3c0 = posA[indiA[33]];
		triA.v3c1 = posA[indiA[34]];
		triA.v3c2 = posA[indiA[35]];
		if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
		{
			return true;
		}

		return false;
	}
	
	static bool TestMeshToMesh(
		Buffer<float3> posA, Buffer<float3> posB,
		Buffer<int> indiA, Buffer<int> indiB,
		float4x4 AfromB
	)
	{
		float3x3 triA = f3x3Zero;
		
		int i = 0;
		for (i = 0; i < 36; i = i + 3)
		{
			triA.v3c0 = posA[indiA[i + 0]];
			triA.v3c1 = posA[indiA[i + 1]];
			triA.v3c2 = posA[indiA[i + 2]];
			if (HHCollider::TestTriangleToMesh(triA, posB, indiB, AfromB))
			{
				return true;
			}
		}
		
		return false;
	}

	static bool TestTriangleToMesh(
		float3x3 triA, Buffer<float3> posB, Buffer<int> indiB, float4x4 AfromB)	
	{
		float3x3 triB = f3x3Zero;

		triB.v3c0 = posB[indiB[0]];
		triB.v3c1 = posB[indiB[1]];
		triB.v3c2 = posB[indiB[2]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[3]];
		triB.v3c1 = posB[indiB[4]];
		triB.v3c2 = posB[indiB[5]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[6]];
		triB.v3c1 = posB[indiB[7]];
		triB.v3c2 = posB[indiB[8]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[9]];
		triB.v3c1 = posB[indiB[10]];
		triB.v3c2 = posB[indiB[11]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[12]];
		triB.v3c1 = posB[indiB[13]];
		triB.v3c2 = posB[indiB[14]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[15]];
		triB.v3c1 = posB[indiB[16]];
		triB.v3c2 = posB[indiB[17]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[18]];
		triB.v3c1 = posB[indiB[19]];
		triB.v3c2 = posB[indiB[20]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[21]];
		triB.v3c1 = posB[indiB[22]];
		triB.v3c2 = posB[indiB[23]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[24]];
		triB.v3c1 = posB[indiB[25]];
		triB.v3c2 = posB[indiB[26]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[27]];
		triB.v3c1 = posB[indiB[28]];
		triB.v3c2 = posB[indiB[29]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[30]];
		triB.v3c1 = posB[indiB[31]];
		triB.v3c2 = posB[indiB[32]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		triB.v3c0 = posB[indiB[33]];
		triB.v3c1 = posB[indiB[34]];
		triB.v3c2 = posB[indiB[35]];
		if (Triangle::TestTriangleToTriangleSimple(triA, triB, AfromB))
		{
			return true;
		}

		return false;
	}
	*/
};

struct Quaternion
{
    static float4 identity()
    {
        return float4(0.0f, 0.0f, 0.0f, 1.0f);
    }
    
    static float4 axisAngle(float3 axis, float angle)
    {
        float sin;
        float cos;
        sincos(0.5f * angle, sin, cos);
        return float4(sin * axis, cos);
    }
	
    static float4 conjugate(float4 q)
    {
        return float4(-q.xyz, q.w);
    }
	
    static float4 mul(float4 a, float4 b)
    {
        return float4(a.xyz * b.w + a.w * b.xyz + cross(a.xyz, b.xyz), -dot(a.xyz, b.xyz) + a.w * b.w);
    }
	
    static float3 rotate(float4 q, float3 v)
    {
        float3 t = 2.0f * cross(q.xyz, v);
        return v + cross(q.xyz, t) + q.w * t;
    }
	
    static float3x3 toMat(float4 q)
    {
        float _2xx = 2.0f * q.x * q.x;
        float _2yy = 2.0f * q.y * q.y;
        float _2zz = 2.0f * q.z * q.z;

        float _2xy = 2.0f * q.x * q.y;
        float _2yz = 2.0f * q.y * q.z;
        float _2zx = 2.0f * q.z * q.x;

        float _2wx = 2.0f * q.w * q.x;
        float _2wy = 2.0f * q.w * q.y;
        float _2wz = 2.0f * q.w * q.z;

        float3x3 R;
        R._m00 = 1.0f - _2yy - _2zz;
        R._m11 = 1.0f - _2zz - _2xx;
        R._m22 = 1.0f - _2yy - _2xx;

        R._m10 = _2xy + _2wz;
        R._m01 = _2xy - _2wz;

        R._m20 = _2zx - _2wy;
        R._m02 = _2zx + _2wy;

        R._m21 = _2yz + _2wx;
        R._m12 = _2yz - _2wx;

        return R;
    }
	
    static float4 fromMat(float3x3 m)
    {
        float4 q;
		
        float trace = m._m00 + m._m11 + m._m22;
        if (trace >= 0)
        {
            q.w = 0.5f * sqrt(trace + 1);
            q.z = (+m._m10 - m._m01) / (4 * q.w);
            q.y = (-m._m20 + m._m02) / (4 * q.w);
            q.x = (+m._m21 - m._m12) / (4 * q.w);
        }
        else
        {
            float dia[3];

            dia[0] = m._m00;
            dia[1] = m._m11;
            dia[2] = m._m22;
			
            float max = dia[0];
            for (int i = 1; i < 3; i++)
            {
                if (max < dia[i])
                {
                    max = dia[i];
                }
            }

            if (max == m._m00)
            {
                q.x = (0.5f) * sqrt(+m._m00 - m._m11 - m._m22 + 1);
                q.y = (+m._m10 + m._m01) / (4 * q.x);
                q.z = (+m._m20 + m._m02) / (4 * q.x);
                q.w = (+m._m21 - m._m12) / (4 * q.x);
            }
            else if (max == m._m11)
            {
                q.y = (0.5f) * sqrt(-m._m00 + m._m11 - m._m22 + 1);
                q.x = (+m._m10 + m._m01) / (4 * q.y);
                q.w = (-m._m20 + m._m02) / (4 * q.y);
                q.z = (+m._m12 + m._m21) / (4 * q.y);
            }
            else if (max == m._m22)
            {
                q.z = (0.5f) * sqrt(-m._m00 - m._m11 + m._m22 + 1);
                q.w = (+m._m10 - m._m01) / (4 * q.z);
                q.x = (+m._m20 + m._m02) / (4 * q.z);
                q.y = (+m._m21 + m._m12) / (4 * q.z);
            }

        }
        return q;
    }
    
    static float4 fromMat(float3 c0, float3 c1, float3 c2)
    {
        float4 q;
        float3x3 m;
        m.v3c0 = c0;
        m.v3c1 = c1;
        m.v3c2 = c2;
        
        q = fromMat(m);
        
        return q;
    }
	
    static float4 slerp(float4 a, float4 b, float t)
    {
        float4 q;
		
        float dt = dot(a, b);
        if (dt < 0.0f)
        {
            dt = -dt;
            b = -b;
        }

        if (dt < 0.9995f)
        {
            float angle = acos(dt);
            float si = rsqrt(1.0f - dt * dt);
            float wa = sin(angle * (1.0f - t)) * si;
            float wb = sin(angle * t) * si;
            return float4(a * wa + b * wb);
        }
        else
        {
            return normalize(lerp(a, b, t));
        }
    }
	
	
	//
    static float3x3 GetRotMatrix(float4 q)
    {
        float _2xx = 2.0f * q.x * q.x;
        float _2yy = 2.0f * q.y * q.y;
        float _2zz = 2.0f * q.z * q.z;

        float _2xy = 2.0f * q.x * q.y;
        float _2yz = 2.0f * q.y * q.z;
        float _2zx = 2.0f * q.z * q.x;

        float _2wx = 2.0f * q.w * q.x;
        float _2wy = 2.0f * q.w * q.y;
        float _2wz = 2.0f * q.w * q.z;

        float3x3 R;
        R._m00 = 1.0f - _2yy - _2zz;
        R._m11 = 1.0f - _2zz - _2xx;
        R._m22 = 1.0f - _2yy - _2xx;

        R._m10 = _2xy + _2wz;
        R._m01 = _2xy - _2wz;

        R._m20 = _2zx - _2wy;
        R._m02 = _2zx + _2wy;

        R._m21 = _2yz + _2wx;
        R._m12 = _2yz - _2wx;

        return R;
    }
		
    static float4 qmul(float4 qa, float4 qb)
    {
        float4 q;
        float4x4 qm;
        qm.v4c0 = qa.x * qb;
        qm.v4c1 = qa.y * qb;
        qm.v4c2 = qa.z * qb;
        qm.v4c3 = qa.w * qb;

        q.x = (+qm.v4c0.w) + (-qm.v4c1.z) + (+qm.v4c2.y) + (+qm.v4c3.x);
        q.y = (+qm.v4c0.z) + (+qm.v4c1.w) + (-qm.v4c2.x) + (+qm.v4c3.y);
        q.z = (-qm.v4c0.y) + (+qm.v4c1.x) + (+qm.v4c2.w) + (+qm.v4c3.z);
        q.w = (-qm.v4c0.x) + (-qm.v4c1.y) + (-qm.v4c2.z) + (+qm.v4c3.w);

        return q;
    }
		
    static float4 slerp2(float4 p, float4 q, float t)
    {
        float4 _p = conjugate(p);
        float4 r = qmul(_p, q);
        if (r.w < 0.0f)
        {
            r = -r;
        }

        if (r.w < 0.995f)
        {
            float angle = acos(r.w);
            float3 axis = normalize(r.xyz);
            float sin = 0.0f;
            float cos = 0.0f;
            sincos(t * angle, sin, cos);
            r = float4(sin, sin, sin, cos) * float4(axis, 1.0f);
            r = qmul(p, r);
        }
        else
        {
            r = normalize(lerp(p, q, t));
        }
            
        return r;
    }
};

struct DualQuaternion
{
    float4 real;
    float4 dual;
      
    //
    void set(float4 _real, float4 _dual)
    {
        real = _real;
        dual = _dual;
    }
    
    //
    void toRigidParam(out float4 r, out float3 t)
    {
        r = 0.0f;
        t = 0.0f;

        {
            r = real;
            float4 _real = float4(-real.xyz, +real.w);
            t = 2.0f * float3(cross(dual.xyz, _real.xyz) + _real.xyz * dual.w + dual.xyz * _real.w);
        }
    }
     
    void toRigidParam(out float3x3 R, out float3 t)
    {
        R = 0.0f;
        t = 0.0f;
            
        {
            float4 r = 0.0f;
            toRigidParam(r, t);           
            R = Quaternion::toMat(r);
        }
    }
     
    void toRigidParam(out float4x4 M)
    {
        float3x3 R = 0.0f;
        float3 t = 0.0f;
        M = 0.0f;

        {
            toRigidParam(R, t);
            M.v4c0 = float4(R.v3c0, 0.0f);
            M.v4c1 = float4(R.v3c1, 0.0f);
            M.v4c2 = float4(R.v3c2, 0.0f);
            M.v4c3 = float4(t, 1.0f);
        }
    }
    
    //   
    void fromRigidParam(float4 r, float3 t)
    {
        real = r;
        dual = 0.5f * Quaternion::mul(float4(t, 0.0f), r);            
    }
      
    void fromRigidParam(float3x3 R, float3 t)
    {
        float4 r = Quaternion::fromMat(R);
        fromRigidParam(r, t);             
    }
       
    void fromRigidParam(float4x4 M)
    {
        float3x3 R = 0.0f;
        float3 t = 0.0f;
        R.v3c0 = M.v3c0;
        R.v3c1 = M.v3c1;
        R.v3c2 = M.v3c2;
        t = M.v3c3;
        fromRigidParam(R, t);                      
    }
    
    //    
    static DualQuaternion conjugate0(DualQuaternion q)
    {
        DualQuaternion p;
        p.real = float4(-q.real.xyz, +q.real.w);
        p.dual = float4(-q.dual.xyz, +q.dual.w);
            
        return p;
    }
        
    static DualQuaternion conjugate1(DualQuaternion q)
    {
        DualQuaternion p;
        p.real = float4(-q.real.xyz, +q.real.w);
        p.dual = float4(+q.dual.xyz, -q.dual.w);
        
        return p;
    }
    
    //
    static DualQuaternion mul(DualQuaternion a, DualQuaternion b)
    {
        DualQuaternion p;

        p.real = Quaternion::mul(a.real, b.real);
        p.dual = 
            Quaternion::mul(a.real, b.dual) +
            Quaternion::mul(a.dual, b.real);
                    
        return p;
    }
    
    static float3 transform(DualQuaternion q, float3 v)
    {              
        float4 r;
        float3 t;
        q.toRigidParam(r, t);
        v = Quaternion::rotate(r, v) + t;
    
        return v;
    }
           
    static DualQuaternion smul(float s, DualQuaternion a)
    {
        DualQuaternion p;
    
        p.real = s * a.real;
        p.dual = s * a.dual;
        
        return p;
    }
           
    static DualQuaternion smul(DualQuaternion a, float s)
    {
        DualQuaternion p;
    
        p.real = a.real * s;
        p.dual = a.dual * s;
        
        return p;
    }
    
    //    
    static float dot(DualQuaternion a, DualQuaternion b)
    {
        return dot(a.real, b.real);
    }
     
    static DualQuaternion Normalize(DualQuaternion q0)
    {
        DualQuaternion q1 = q0;

        float mag = dot(q0.real, q0.real);

        if (mag > 0.000001f)
        {
                //assert
        }
        q1.real *= 1.0f / mag;
        q1.dual *= 1.0f / mag;

        return q1;
    }
   
    static DualQuaternion Lerp(DualQuaternion a, DualQuaternion b, float dt)
    {
        DualQuaternion q;

        float3 ta;
        float4 ra;
        float3 tb;
        float4 rb;
        float3 t;
        float4 r;

        a.toRigidParam(ra, ta);
        b.toRigidParam(rb, tb);

        t = lerp(ta, tb, dt);
        r = Quaternion::slerp(ra, rb, dt);           

        q.fromRigidParam(r, t);

        return q;
    }
    
    //   
    void toScrewParam(out float o, out float d, out float3 l, out float3 m)
    {
        o = 0.0f;
        d = 0.0f;
        l = 0.0f;
        m = 0.0f;

        {
            float i_s = 1.0f / length(real.xyz);

            o = +2.0f * acos(real.w);
            d = -2.0f * dual.w * i_s;
            l = real.xyz * i_s;
            m = (dual.xyz - l * d * real.w * 0.5f) * i_s;
        }
    }
    
    void fromScrewParam(float o, float d, float3 l, float3 m)
    {
        float o_2 = 0.5f * o;
        float d_2 = 0.5f * d;
        float sin = 0.0f;
        float cos = 0.0f;

        {
            sincos(o_2, sin, cos);

            real.xyz = sin * l;
            real.w = cos;

            dual.xyz = sin * m + d_2 * cos * l;
            dual.w = -d_2 * sin;
        }
    }
    
    //
    static DualQuaternion scLerp1(DualQuaternion a, DualQuaternion b, float t)
    {
        DualQuaternion q;
        DualQuaternion dq;
        float o = 0.0f;
        float d = 0.0f;
        float3 l = 0.0f;
        float3 m = 0.0f;
               
        DualQuaternion _b = b;        
        if (dot(a.real, b.real) < 0.0f)
        {
            _b = DualQuaternion::smul((-1.0f), b);                        
        }
        DualQuaternion _a = DualQuaternion::conjugate0(a);
        
        dq = DualQuaternion::mul(_a, _b);
        
        if (dq.real.w < 1.0f)
        {                       
            dq.toScrewParam(o, d, l, m);
            o = o * t;
            d = d * t;
            dq.fromScrewParam(o, d, l, m);
        
            q = DualQuaternion::mul(a, dq);
        }
        else
        {
            q = DualQuaternion::Lerp(a, b, t);
        }
        
        ////DualQuaternion b = (dot(a.real, b.real) >= 0.0f ? b : DualQuaternion::smul((-1.0f), b));
        //DualQuaternion _b = b;
        ////_b.real = b.real;
        ////_b.dual = b.dual;                
        //
        //if (dot(a.real, b.real) < 0.0f)
        //{
        //    _b = DualQuaternion::smul((-1.0f), b);
        //}
        //DualQuaternion _a = DualQuaternion::conjugate0(a);
        //
        //dq = DualQuaternion::mul(_a, _b);
        //
        //if (dq.real.w < 0.9f)
        //{
        //    dq.toScrewParam(o, d, l, m);
        //    o = o * t;
        //    d = d * t;
        //    dq.fromScrewParam(o, d, l, m);
        //
        //    q = DualQuaternion::mul(a, dq);
        //}
        //else
        //{
        //    q = DualQuaternion::Lerp(a, b, t);
        //}
                        
        return q;
    }
    
    static DualQuaternion scLerp(DualQuaternion a, DualQuaternion b, float t)
    {
        DualQuaternion q;
        DualQuaternion dq;
        float o = 0.0f;
        float d = 0.0f;
        float3 l = 0.0f;
        float3 m = 0.0f;
                   
        //DualQuaternion b = (dot(a.real, b.real) >= 0.0f ? b : DualQuaternion::smul((-1.0f), b));
        DualQuaternion _b = b;      
        
        if (dot(a.real, b.real) < 0.0f)
        {
            _b = DualQuaternion::smul((-1.0f), b);
        }
        DualQuaternion _a = DualQuaternion::conjugate0(a);
        
        dq = DualQuaternion::mul(_a, _b);
        
        if (dq.real.w < 0.9f)
        {
            dq.toScrewParam(o, d, l, m);
            o = o * t;
            d = d * t;
            dq.fromScrewParam(o, d, l, m);
        
            q = DualQuaternion::mul(a, dq);
        }
        else
        {
            q = DualQuaternion::Lerp(a, b, t);
        }
                        
        return q;
    }
    
    static DualQuaternion scLerp_debug(DualQuaternion a, DualQuaternion b, float t, out float3 rp, out float3 rb, out float3 rl)
    {
        DualQuaternion q;
        DualQuaternion dq;
        float o = 0.0f;
        float d = 0.0f;
        float3 l = 0.0f;
        float3 m = 0.0f;
    
        //DualQuaternion b = (dot(a.real, b.real) >= 0.0f ? b : DualQuaternion::smul((-1.0f), b));
        DualQuaternion _b = b;
        if (dot(a.real, b.real) < 0.0f)
        {
            _b = DualQuaternion::smul((-1.0f), b);
        }
        DualQuaternion _a = DualQuaternion::conjugate0(a);
        
        dq = DualQuaternion::mul(_a, _b);
    
        if (dq.real.w < 1.0f)
        {
            dq.toScrewParam(o, d, l, m);
            o = o * t;
            d = d * t;
            dq.fromScrewParam(o, d, l, m);
    
            q = DualQuaternion::mul(a, dq);
        }
        else
        {
            q = DualQuaternion::Lerp(a, b, t);
        }
    
        //Debug
        float4 rot;
        float3 tb;
        float3 p;
        
        p = cross(l, m);
        dq.toRigidParam(rot, tb);
    
        rp = DualQuaternion::transform(a, p);
        rb = DualQuaternion::transform(a, (tb - d * l));
        rl = DualQuaternion::transform(a, (p + d * l));
    
        return q;
    }
        
    static DualQuaternion scLerp0(DualQuaternion a, DualQuaternion b, float t)
    {
        DualQuaternion q;
        DualQuaternion dq;
        float o = 0.0f;
        float d = 0.0f;
        float3 l = 0.0f;
        float3 m = 0.0f;
    
        //DualQuaternion b = (dot(a.real, b.real) >= 0.0f ? b : DualQuaternion::smul((-1.0f), b));
        DualQuaternion _b = b;
        if (dot(a.real, b.real) < 0.0f)
        {
            _b = DualQuaternion::smul((-1.0f), b);
        }
        DualQuaternion _a = DualQuaternion::conjugate0(a);
        
        dq = DualQuaternion::mul(_a, _b);
    
        dq.toScrewParam(o, d, l, m);
        o = o * t;
        d = d * t;
        dq.fromScrewParam(o, d, l, m);
    
        q = DualQuaternion::mul(a, dq);
    
        return q;
    }
    
    static DualQuaternion scLerp_debug0(DualQuaternion a, DualQuaternion b, float t, out float3 rp, out float3 rb, out float3 rl)
    {
        DualQuaternion q ;
        DualQuaternion dq;
        float o = 0.0f;
        float d = 0.0f;
        float3 l = 0.0f;
        float3 m = 0.0f;
    
        //DualQuaternion b = (dot(a.real, b.real) >= 0.0f ? b : DualQuaternion::smul((-1.0f), b));
        DualQuaternion _b = b;
        if (dot(a.real, b.real) < 0.0f)
        {
            _b = DualQuaternion::smul((-1.0f), b);
        }
        DualQuaternion _a = DualQuaternion::conjugate0(a);
        
        dq = DualQuaternion::mul(_a, _b);
    
        dq.toScrewParam(o, d, l, m);
        o = o * t;
        d = d * t;
        dq.fromScrewParam(o, d, l, m);
    
        q = DualQuaternion::mul(a, dq);
    
        //Debug
        float4 rot;
        float3 tb;
        float3 p;
        
        p = cross(l, m);
        dq.toRigidParam(rot, tb);
    
        rp = DualQuaternion::transform(a, p);
        rb = DualQuaternion::transform(a, (tb - d * l));
        rl = DualQuaternion::transform(a, (p + d * l));
    
        return q;
    }                   
    
   
};

struct Transform
{
    static float4x4 GetAfromB(
		float3 ta, float4 qa, float3 sa,
		float3 tb, float4 qb, float3 sb)
    {
        float4x4 AfromB;

        float3x3 Ra = Quaternion::GetRotMatrix(qa);
        float3x3 Rb = Quaternion::GetRotMatrix(qb);

        float4x4 A;
        float4x4 B;
		//A
        float3 sai = 1.0f / sa;
        A.v3r0 = (sai.x) * Ra.v3c0;
        A.v3r1 = (sai.y) * Ra.v3c1;
        A.v3r2 = (sai.z) * Ra.v3c2;
        A.v3r3 = float3(0.0f, 0.0f, 0.0f);

        A.v4c3.x = dot(A.v3r0, -ta);
        A.v4c3.y = dot(A.v3r1, -ta);
        A.v4c3.z = dot(A.v3r2, -ta);
        A.v4c3.w = 1.0f;

		//B
        B.v4c0 = float4(sb.x * Rb.v3c0, 0.0f);
        B.v4c1 = float4(sb.y * Rb.v3c1, 0.0f);
        B.v4c2 = float4(sb.z * Rb.v3c2, 0.0f);

        B.v4c3 = float4(tb, 1.0f);

        AfromB = mul(A, B);
        return AfromB;
    }

    static float3x3 GetAfromBnormal(
		float4 qa, float3 sa,
		float4 qb, float3 sb)
    {
        float3x3 AfromBnormal;

        float3x3 Ra = Quaternion::GetRotMatrix(qa);
        float3x3 Rb = Quaternion::GetRotMatrix(qb);

        float3x3 A;
        float3x3 B;
		//A
        A.v3r0 = (sa.x) * Ra.v3c0;
        A.v3r1 = (sa.y) * Ra.v3c1;
        A.v3r2 = (sa.z) * Ra.v3c2;

		//B
        float3 sbi = 1.0 / sb;
        B.v3c0 = float3(sbi.x * Rb.v3c0);
        B.v3c1 = float3(sbi.y * Rb.v3c1);
        B.v3c2 = float3(sbi.z * Rb.v3c2);

        AfromBnormal = mul(A, B);
        return AfromBnormal;
    }

    static void GetAfromB_AfromBn(
		float3 ta, float4 qa, float3 sa,
		float3 tb, float4 qb, float3 sb,
		out float4x4 AfromB, out float3x3 AfromBn)
    {
        float3x3 Ra = Quaternion::GetRotMatrix(qa);
        float3x3 Rb = Quaternion::GetRotMatrix(qb);

		////AfromB
        float4x4 A;
        float4x4 B;
		//A
        float3 sai = 1.0f / sa;
        A.v3r0 = (sai.x) * Ra.v3c0;
        A.v3r1 = (sai.y) * Ra.v3c1;
        A.v3r2 = (sai.z) * Ra.v3c2;
        A.v3r3 = float3(0.0f, 0.0f, 0.0f);

        A.v4c3.x = dot(A.v3r0, -ta);
        A.v4c3.y = dot(A.v3r1, -ta);
        A.v4c3.z = dot(A.v3r2, -ta);
        A.v4c3.w = 1.0f;

		//B
        B.v4c0 = float4(sb.x * Rb.v3c0, 0.0f);
        B.v4c1 = float4(sb.y * Rb.v3c1, 0.0f);
        B.v4c2 = float4(sb.z * Rb.v3c2, 0.0f);

        B.v4c3 = float4(tb, 1.0f);

        AfromB = mul(A, B);

		////AfromBn
        float3x3 An;
        float3x3 Bn;
		//A
        An.v3r0 = (sa.x) * Ra.v3c0;
        An.v3r1 = (sa.y) * Ra.v3c1;
        An.v3r2 = (sa.z) * Ra.v3c2;

		//B
        float3 sbi = 1.0 / sb;
        Bn.v3c0 = float3(sbi.x * Rb.v3c0);
        Bn.v3c1 = float3(sbi.y * Rb.v3c1);
        Bn.v3c2 = float3(sbi.z * Rb.v3c2);

        AfromBn = mul(An, Bn);
        return;
    }
	
    static void GetAfromB_AfromBn(
		float3 ta, float4 qa, float3 sa,
		float3 ta0, float4 qa0, float3 sa0,
		float3 tb, float4 qb, float3 sb,
		out float4x4 AfromB, out float3x3 AfromBn)
    {
        float3x3 Ra = Quaternion::GetRotMatrix(qa);
        float3x3 Ra0 = Quaternion::GetRotMatrix(qa0);
        float3x3 Rb = Quaternion::GetRotMatrix(qb);

		////AfromB        
        float4x4 A;
        float4x4 A0;
        float4x4 B;
				
		//A
        float3 sai = 1.0f / sa;
        A.v3r0 = (sai.x) * Ra.v3c0;
        A.v3r1 = (sai.y) * Ra.v3c1;
        A.v3r2 = (sai.z) * Ra.v3c2;
        A.v3r3 = float3(0.0f, 0.0f, 0.0f);

        A.v4c3.x = dot(A.v3r0, -ta);
        A.v4c3.y = dot(A.v3r1, -ta);
        A.v4c3.z = dot(A.v3r2, -ta);
        A.v4c3.w = 1.0f;

		//A0
        float3 sa0i = 1.0f / sa0;
        A0.v3r0 = (sa0i.x) * Ra0.v3c0;
        A0.v3r1 = (sa0i.y) * Ra0.v3c1;
        A0.v3r2 = (sa0i.z) * Ra0.v3c2;
        A0.v3r3 = float3(0.0f, 0.0f, 0.0f);

        A0.v4c3.x = dot(A0.v3r0, -ta0);
        A0.v4c3.y = dot(A0.v3r1, -ta0);
        A0.v4c3.z = dot(A0.v3r2, -ta0);
        A0.v4c3.w = 1.0f;
		
		//B
        B.v4c0 = float4(sb.x * Rb.v3c0, 0.0f);
        B.v4c1 = float4(sb.y * Rb.v3c1, 0.0f);
        B.v4c2 = float4(sb.z * Rb.v3c2, 0.0f);

        B.v4c3 = float4(tb, 1.0f);

        AfromB = mul(mul(A, A0), B);

		////AfromBn        
        float3x3 An;
        float3x3 An0;
        float3x3 Bn;
				
		//A
        An.v3r0 = (sa.x) * Ra.v3c0;
        An.v3r1 = (sa.y) * Ra.v3c1;
        An.v3r2 = (sa.z) * Ra.v3c2;

		//A0
        An0.v3r0 = (sa0.x) * Ra0.v3c0;
        An0.v3r1 = (sa0.y) * Ra0.v3c1;
        An0.v3r2 = (sa0.z) * Ra0.v3c2;
				
		//B
        float3 sbi = 1.0 / sb;
        Bn.v3c0 = float3(sbi.x * Rb.v3c0);
        Bn.v3c1 = float3(sbi.y * Rb.v3c1);
        Bn.v3c2 = float3(sbi.z * Rb.v3c2);

        AfromBn = mul(mul(An, An0), Bn);
        return;
    }
	
    static void GetAfromB_AfromBn_W_Wn(
		float3 ta, float4 qa, float3 sa,
		float3 tb, float4 qb, float3 sb,
		out float4x4 AfromB, out float3x3 AfromBn, out float4x4 W, out float3x3 Wn)
    {
        float3x3 Ra = Quaternion::GetRotMatrix(qa);
        float3x3 Rb = Quaternion::GetRotMatrix(qb);

		////AfromB
        float4x4 A;
        float4x4 B;
		//A
        float3 sai = 1.0f / sa;
        A.v3r0 = (sai.x) * Ra.v3c0;
        A.v3r1 = (sai.y) * Ra.v3c1;
        A.v3r2 = (sai.z) * Ra.v3c2;
        A.v3r3 = float3(0.0f, 0.0f, 0.0f);

        A.v4c3.x = dot(A.v3r0, -ta);
        A.v4c3.y = dot(A.v3r1, -ta);
        A.v4c3.z = dot(A.v3r2, -ta);
        A.v4c3.w = 1.0f;

		//B
        B.v4c0 = float4(sb.x * Rb.v3c0, 0.0f);
        B.v4c1 = float4(sb.y * Rb.v3c1, 0.0f);
        B.v4c2 = float4(sb.z * Rb.v3c2, 0.0f);

        B.v4c3 = float4(tb, 1.0f);

        AfromB = mul(A, B);
        W = B;

		////AfromBn
        float3x3 An;
        float3x3 Bn;
		//A
        An.v3r0 = (sa.x) * Ra.v3c0;
        An.v3r1 = (sa.y) * Ra.v3c1;
        An.v3r2 = (sa.z) * Ra.v3c2;

		//B
        float3 sbi = 1.0 / sb;
        Bn.v3c0 = float3(sbi.x * Rb.v3c0);
        Bn.v3c1 = float3(sbi.y * Rb.v3c1);
        Bn.v3c2 = float3(sbi.z * Rb.v3c2);

        AfromBn = mul(An, Bn);
        Wn = Bn;
		
        return;
    }
	
    static void GetW_Wn(
		float3 t, float4 q, float3 s,
		out float4x4 W, out float3x3 Wn)
    {
        float3x3 R = Quaternion::GetRotMatrix(q);
		
		//M
        float4x4 M;
        M.v4c0 = float4(s.x * R.v3c0, 0.0f);
        M.v4c1 = float4(s.y * R.v3c1, 0.0f);
        M.v4c2 = float4(s.z * R.v3c2, 0.0f);
        M.v4c3 = float4(t, 1.0f);
        W = M;

		//Mn
        float3x3 Mn;
        float3 si = 1.0 / s;
        Mn.v3c0 = float3(si.x * R.v3c0);
        Mn.v3c1 = float3(si.y * R.v3c1);
        Mn.v3c2 = float3(si.z * R.v3c2);
        Wn = Mn;
		
        return;
    }
	
    static void GetW_Wn(
		float3 t0, float4 q0, float3 s0,
		float3 t, float4 q, float3 s,
		out float4x4 W, out float3x3 Wn)
    {
        float3x3 R0 = Quaternion::GetRotMatrix(q0);
        float3x3 R = Quaternion::GetRotMatrix(q);
        		
		//M0
        float4x4 M0;
        M0.v4c0 = float4(s0.x * R0.v3c0, 0.0f);
        M0.v4c1 = float4(s0.y * R0.v3c1, 0.0f);
        M0.v4c2 = float4(s0.z * R0.v3c2, 0.0f);
        M0.v4c3 = float4(t0, 1.0f);
		
		//M
        float4x4 M;
        M.v4c0 = float4(s.x * R.v3c0, 0.0f);
        M.v4c1 = float4(s.y * R.v3c1, 0.0f);
        M.v4c2 = float4(s.z * R.v3c2, 0.0f);
        M.v4c3 = float4(t, 1.0f);
        W = mul(M0, M);

		//Mn0
        float3x3 Mn0;
        float3 s0i = 1.0 / s0;
        Mn0.v3c0 = float3(s0i.x * R0.v3c0);
        Mn0.v3c1 = float3(s0i.y * R0.v3c1);
        Mn0.v3c2 = float3(s0i.z * R0.v3c2);
		
		//Mn
        float3x3 Mn;
        float3 si = 1.0 / s;
        Mn.v3c0 = float3(si.x * R.v3c0);
        Mn.v3c1 = float3(si.y * R.v3c1);
        Mn.v3c2 = float3(si.z * R.v3c2);
        Wn = mul(Mn0, Mn);
		
        return;
    }
	
    static void GetW_Wn(
		float3 t, float3x3 R, float3 s,
		out float4x4 W, out float3x3 Wn)
    {
        //float3x3 R = Quaternion::GetRotMatrix(q);
		
		//M
        float4x4 M;
        M.v4c0 = float4(s.x * R.v3c0, 0.0f);
        M.v4c1 = float4(s.y * R.v3c1, 0.0f);
        M.v4c2 = float4(s.z * R.v3c2, 0.0f);
        M.v4c3 = float4(t, 1.0f);
        W = M;

		//Mn
        float3x3 Mn;
        float3 si = 1.0 / s;
        Mn.v3c0 = float3(si.x * R.v3c0);
        Mn.v3c1 = float3(si.y * R.v3c1);
        Mn.v3c2 = float3(si.z * R.v3c2);
        Wn = Mn;
		
        return;
    }
    
    static float4x4 GetW(
		float3 t, float4 q, float3 s)		
    {        
        float3x3 R = Quaternion::GetRotMatrix(q);
		
		//M
        float4x4 M;
        M.v4c0 = float4(s.x * R.v3c0, 0.0f);
        M.v4c1 = float4(s.y * R.v3c1, 0.0f);
        M.v4c2 = float4(s.z * R.v3c2, 0.0f);
        M.v4c3 = float4(t, 1.0f);       		
		
        return M;
    }
    
    static float3x3 GetWn(
		float3 t, float4 q, float3 s)
    {
        float3x3 R = Quaternion::GetRotMatrix(q);
				
		//Mn
        float3x3 Mn;
        float3 si = 1.0 / s;
        Mn.v3c0 = float3(si.x * R.v3c0);
        Mn.v3c1 = float3(si.y * R.v3c1);
        Mn.v3c2 = float3(si.z * R.v3c2);       
		
        return Mn;
    }
    
    static float MaxScale(float3 s)
    {        
        return max(max(s.x, s.y), s.z);
    }
    
    static float4x4 GetTr(float4x4 M)
    {        
        float4x4 tr = 0.0f;
        
        float3 sca;
        sca.x = length(M.v3c0);
        sca.y = length(M.v3c1);
        sca.z = length(M.v3c2);

        float3x3 R;
        R.v3c0 = M.v3c0 / sca.x;
        R.v3c1 = M.v3c1 / sca.y;
        R.v3c2 = M.v3c2 / sca.z;
        float4 rot = Quaternion::fromMat(R);

        float3 pos;
        pos.x = M.v3c3.x;
        pos.y = M.v3c3.y;
        pos.z = M.v3c3.z;

        tr.v3c0= pos;
        tr.v4c1 = rot;
        tr.v3c2= sca;

        return tr;
    }
};

struct OBB
{
    static bool TestOBBtoOBB(float4x4 A, float4x4 B)
    {
        const float3x3 I = float3x3(1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
        const float3x3 ONE = float3x3(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f);
        const float3 zero = float3(0.0f, 0.0f, 0.0f);

        const float epsilon = 0.001f;
        float3x3 E = ONE * epsilon;

        float3 ta = float3(A[0][3], A[1][3], A[2][3]);
        float3 tb = float3(B[0][3], B[1][3], B[2][3]);

        float3x3 Ra = I;
        float3x3 Rb = I;

        float3 sa = zero;
        float3 sb = zero;
	
        float3 col0 = zero;
        float3 col1 = zero;
        float3 col2 = zero;
        float l0 = 0.0f;
        float l1 = 0.0f;
        float l2 = 0.0f;
	//Ra & sa
        col0 = float3(A[0][0], A[1][0], A[2][0]);
        col1 = float3(A[0][1], A[1][1], A[2][1]);
        col2 = float3(A[0][2], A[1][2], A[2][2]);
        l0 = sqrt(dot(col0, col0));
        l1 = sqrt(dot(col1, col1));
        l2 = sqrt(dot(col2, col2));
        sa[0] = abs(0.5f * l0);
        sa[1] = abs(0.5f * l1);
        sa[2] = abs(0.5f * l2);
        col0 = (1.0f / l0) * col0;
        col1 = (1.0f / l1) * col1;
        col2 = (1.0f / l2) * col2;
        Ra[0][0] = col0[0];
        Ra[0][1] = col1[0];
        Ra[0][2] = col2[0];
        Ra[1][0] = col0[1];
        Ra[1][1] = col1[1];
        Ra[1][2] = col2[1];
        Ra[2][0] = col0[2];
        Ra[2][1] = col1[2];
        Ra[2][2] = col2[2];
	
	//Rb & sb
        col0 = float3(B[0][0], B[1][0], B[2][0]);
        col1 = float3(B[0][1], B[1][1], B[2][1]);
        col2 = float3(B[0][2], B[1][2], B[2][2]);
        l0 = sqrt(dot(col0, col0));
        l1 = sqrt(dot(col1, col1));
        l2 = sqrt(dot(col2, col2));
        sb[0] = abs(0.5f * l0);
        sb[1] = abs(0.5f * l1);
        sb[2] = abs(0.5f * l2);
        col0 = (1.0f / l0) * col0;
        col1 = (1.0f / l1) * col1;
        col2 = (1.0f / l2) * col2;
        Rb[0][0] = col0[0];
        Rb[0][1] = col1[0];
        Rb[0][2] = col2[0];
        Rb[1][0] = col0[1];
        Rb[1][1] = col1[1];
        Rb[1][2] = col2[1];
        Rb[2][0] = col0[2];
        Rb[2][1] = col1[2];
        Rb[2][2] = col2[2];


        int i = 0;
        int j = 0;
	/*
	for (i = 0; i < 3; i++)
	{
		float3 colA = float3(A[0][i], A[1][i], A[2][i]);
		float lenA = length(colA);
		sa[i] = 0.5f * lenA;
		colA = mul((1.0f / lenA), colA);

		float3 colB = float3(B[0][i], B[1][i], B[2][i]);
		float lenB = length(colB);
		sb[i] = 0.5f * lenB;
		colB = mul((1.0f / lenB), colB);
		
	
		for (j = 0; j < 3; j++)
		{
			Ra[j][i] = colA[j];
			Rb[j][i] = colB[j];
			E[j][i] = epsilon;
		}
	}
	*/

        Ra = transpose(Ra);
        float3x3 R = mul(Ra, Rb);
        float3x3 absR = abs(R) + E;
        float3 c = mul(Ra, tb - ta);

        float rc = 0.0f;
        float ra = 0.0f;
        float rb = 0.0f;
	
	//Ra
        rc = abs(c[0]);
        ra = sa[0];
        rb = absR[0][0] * sb[0] + absR[0][1] * sb[1] + absR[0][2] * sb[2];
        if (rc > ra + rb)
            return false;
	

        rc = abs(c[1]);
        ra = sa[1];
        rb = absR[1][0] * sb[0] + absR[1][1] * sb[1] + absR[1][2] * sb[2];
        if (rc > ra + rb)
            return false;
	

        rc = abs(c[2]);
        ra = sa[2];
        rb = absR[2][0] * sb[0] + absR[2][1] * sb[1] + absR[2][2] * sb[2];
        if (rc > ra + rb)
            return false;

	//Rb	
        rc = abs(c[0] * R[0][0] + c[1] * R[1][0] + c[2] * R[2][0]);
        ra = sa[0] * absR[0][0] + sa[1] * absR[1][0] + sa[2] * absR[2][0];
        rb = sb[0];
        if (rc > ra + rb)
            return false;

        rc = abs(c[0] * R[0][1] + c[1] * R[1][1] + c[2] * R[2][1]);
        ra = sa[0] * absR[0][1] + sa[1] * absR[1][1] + sa[2] * absR[2][1];
        rb = sb[1];
        if (rc > ra + rb)
            return false;

        rc = abs(c[0] * R[0][2] + c[1] * R[1][2] + c[2] * R[2][2]);
        ra = sa[0] * absR[0][2] + sa[1] * absR[1][2] + sa[2] * absR[2][2];
        rb = sb[2];
        if (rc > ra + rb)
            return false;

	//Ra.c0 X Rb
        rc = abs(c[1] * (-R[2][0]) + c[2] * R[1][0]);
        ra = sa[1] * absR[2][0] + sa[2] * absR[1][0];
        rb = absR[0][2] * sb[1] + absR[0][1] * sb[2];
        if (rc > ra + rb)
            return false;

        rc = abs(c[1] * (-R[2][1]) + c[2] * R[1][1]);
        ra = sa[1] * absR[2][1] + sa[2] * absR[1][1];
        rb = absR[0][2] * sb[0] + absR[0][0] * sb[2];
        if (rc > ra + rb)
            return false;

        rc = abs(c[1] * (-R[2][2]) + c[2] * R[1][2]);
        ra = sa[1] * absR[2][2] + sa[2] * absR[1][2];
        rb = absR[0][1] * sb[0] + absR[0][0] * sb[1];
        if (rc > ra + rb)
            return false;
	
	//Ra.c1 X Rb
        rc = abs(c[0] * R[2][0] + c[2] * (-R[0][0]));
        ra = sa[0] * absR[2][0] + sa[2] * absR[0][0];
        rb = absR[1][2] * sb[1] + absR[1][1] * sb[2];
        if (rc > ra + rb)
            return false;

        rc = abs(c[0] * R[2][1] + c[2] * (-R[0][1]));
        ra = sa[0] * absR[2][1] + sa[2] * absR[0][1];
        rb = absR[1][2] * sb[0] + absR[1][0] * sb[2];
        if (rc > ra + rb)
            return false;

        rc = abs(c[0] * R[2][2] + c[2] * (-R[0][2]));
        ra = sa[0] * absR[2][2] + sa[2] * absR[0][2];
        rb = absR[1][1] * sb[0] + absR[1][0] * sb[1];
        if (rc > ra + rb)
            return false;
	
	//Ra.c2 X Rb
        rc = abs(c[0] * (-R[1][0]) + c[1] * R[0][0]);
        ra = sa[0] * absR[1][0] + sa[1] * absR[0][0];
        rb = absR[2][2] * sb[1] + absR[2][1] * sb[2];
        if (rc > ra + rb)
            return false;

        rc = abs(c[0] * (-R[1][1]) + c[1] * R[0][1]);
        ra = sa[0] * absR[1][1] + sa[1] * absR[0][1];
        rb = absR[2][2] * sb[0] + absR[2][0] * sb[2];
        if (rc > ra + rb)
            return false;

        rc = abs(c[0] * (-R[1][2]) + c[1] * R[0][2]);
        ra = sa[0] * absR[1][2] + sa[1] * absR[0][2];
        rb = absR[2][1] * sb[0] + absR[2][0] * sb[1];
        if (rc > ra + rb)
            return false;

        return true;
    }
};

struct CurveHermite
{
    static float3 Curve(float3 p0, float3 p1, float3 dp0, float3 dp1, float u)
    {
        float4 U;
        float4x4 M;
        float3x4 G;
        float3 R;

        U.x = u * u * u;
        U.y = u * u;
        U.z = u;
        U.w = 1.0f;

        M._m00 = 2.0f;
        M._m01 = -3.0f;
        M._m02 = 0.0f;
        M._m03 = 1.0f;
        
        M._m10 = -2.0f;
        M._m11 = 3.0f;
        M._m12 = 0.0f;
        M._m13 = 0.0f;
        
        M._m20 = 1.0f;
        M._m21 = -2.0f;
        M._m22 = 1.0f;
        M._m23 = 0.0f;
        
        M._m30 = 1.0f;
        M._m31 = -1.0f;
        M._m32 = 0.0f;
        M._m33 = 0.0f;

        
        G._m00 = p0.x;
        G._m01 = p1.x;
        G._m02 = dp0.x;        
        G._m03 = dp1.x;
        
        G._m10 = p0.y;
        G._m11 = p1.y;
        G._m12 = dp0.y;
        G._m13 = dp1.y;
        
        G._m20 = p0.z;
        G._m21 = p1.z;
        G._m22 = dp0.z;
        G._m23 = dp1.z;

        R = mul(G, mul(M, U));

        return R;
    }

    static float3 TCurve(float3 p0, float3 p1, float3 dp0, float3 dp1, float u)
    {
        float4 U;
        float4x4 M;
        float3x4 G;
        float3 R;

        U.x = 3 * u * u;
        U.y = 2 * u;
        U.z = 1.0f;
        U.w = 0.0f;

        M._m00 = 2.0f;
        M._m01 = -3.0f;
        M._m02 = 0.0f;
        M._m03 = 1.0f;
        
        M._m10 = -2.0f;
        M._m11 = 3.0f;
        M._m12 = 0.0f;
        M._m13 = 0.0f;
        
        M._m20 = 1.0f;
        M._m21 = -2.0f;
        M._m22 = 1.0f;
        M._m23 = 0.0f;
        
        M._m30 = 1.0f;
        M._m31 = -1.0f;
        M._m32 = 0.0f;
        M._m33 = 0.0f;

        
        G._m00 = p0.x;
        G._m01 = p1.x;
        G._m02 = dp0.x;
        G._m03 = dp1.x;
       
        G._m10 = p0.y;
        G._m11 = p1.y;
        G._m12 = dp0.y;
        G._m13 = dp1.y;
        
        G._m20 = p0.z;
        G._m21 = p1.z;
        G._m22 = dp0.z;
        G._m23 = dp1.z;

        R = mul(G, mul(M, U));

        return R;
    }

    static float3 NCurve(float3 p0, float3 p1, float3 dp0, float3 dp1, float u)
    {
        float4 U;
        float4x4 M;
        float3x4 G;
        float3 R;

        U.x = 3 * 2 * u;
        U.y = 2 * 1;
        U.z = 0.0f;
        U.w = 0.0f;

        M._m00 = 2.0f;
        M._m01 = -3.0f;
        M._m02 = 0.0f;
        M._m03 = 1.0f;
        
        M._m10 = -2.0f;        
        M._m11 = 3.0f;
        M._m12 = 0.0f;
        M._m13 = 0.0f;
        
        M._m20 = 1.0f;
        M._m21 = -2.0f;
        M._m22 = 1.0f;
        M._m23 = 0.0f;
        
        M._m30 = 1.0f;
        M._m31 = -1.0f;
        M._m32 = 0.0f;
        M._m33 = 0.0f;

        
        G._m00 = p0.x;
        G._m01 = p1.x;
        G._m02 = dp0.x;
        G._m03 = dp1.x;
        
        G._m10 = p0.y;
        G._m11 = p1.y;
        G._m12 = dp0.y;
        G._m13 = dp1.y;
        
        G._m20 = p0.z;
        G._m21 = p1.z;
        G._m22 = dp0.z;
        G._m23 = dp1.z;

        R = mul(G, mul(M, U));

        return R;
    }

    static float3 BCurve(float3 p0, float3 p1, float3 dp0, float3 dp1, float u)
    {
        float3 T;
        float3 N;
        float3 R;

        T = CurveHermite::TCurve(p0, p1, dp0, dp1, u);
        N = CurveHermite::NCurve(p0, p1, dp0, dp1, u);
        R = cross(T, N);

        return R;
    }
            
    static float GetArcLength(float3 p0, float3 p1, float3 dp0, float3 dp1, float delta)
    {
        float length = 0.0f;
        float3 pos1 = f3zero;
        
        float3 pos2 = f3zero;
        float u = 0.0f;

        while (u <= 1.0f)
        {
            pos1 = Curve(p0, p1, dp0, dp1, u);
            u = u + delta;
            pos2 = Curve(p0, p1, dp0, dp1, u);
            length = length + distance(pos1, pos2);
        }

        return length;
    }
    
    
    //
    static float3x4 GetGM(float3 p0, float3 p1, float3 dp0, float3 dp1)
    {        
        float4x4 M;
        float3x4 G;
        
        float3x4 GM;       

        M._m00 = 2.0f;
        M._m01 = -3.0f;
        M._m02 = 0.0f;
        M._m03 = 1.0f;
        
        M._m10 = -2.0f;
        M._m11 = 3.0f;
        M._m12 = 0.0f;
        M._m13 = 0.0f;
        
        M._m20 = 1.0f;
        M._m21 = -2.0f;
        M._m22 = 1.0f;
        M._m23 = 0.0f;
        
        M._m30 = 1.0f;
        M._m31 = -1.0f;
        M._m32 = 0.0f;
        M._m33 = 0.0f;

        
        G._m00 = p0.x;
        G._m01 = p1.x;
        G._m02 = dp0.x;
        G._m03 = dp1.x;
        
        G._m10 = p0.y;
        G._m11 = p1.y;
        G._m12 = dp0.y;
        G._m13 = dp1.y;
        
        G._m20 = p0.z;
        G._m21 = p1.z;
        G._m22 = dp0.z;
        G._m23 = dp1.z;

        GM = mul(G, M);

        return GM;
    }
    
    
    static float3 CurveGM(float3x4 GM, float u)
    {
        float3 R;
        
        float4 U;
        
        U.x = u * u * u;
        U.y = u * u;
        U.z = u;
        U.w = 1.0f;
        
        R = mul(GM, U);
        
        return R;
    }
    
    static float3 TCurveGM(float3x4 GM, float u)
    {
        float3 R;
        
        float4 U;     
        
        U.x = 3 * u * u;
        U.y = 2 * u;
        U.z = 1.0f;
        U.w = 0.0f;
        
        R = mul(GM, U);
        
        return R;
    }
    
    static float3 NCurveGM(float3x4 GM, float u)
    {
        float3 R;
        
        float4 U;
        
        U.x = 3 * 2 * u;
        U.y = 2 * 1;
        U.z = 0.0f;
        U.w = 0.0f;
        
        R = mul(GM, U);
        
        return R;
    }
    
    
    static float3 BCurveGM(float3x4 GM, float u)
    {       
        float3 T;
        float3 N;
        float3 B;

        T = TCurveGM(GM, u);
        N = NCurveGM(GM, u);
        B = cross(T, N);
        
        return B;
    }
    
    static float3 BCurveTN(float3 T, float3 N, float u)
    {
        float3 B;
                                
        B = cross(T, N);
        
        return B;
    }
    
    
    static float GetArcLengthGM(float3x4 GM, float delta)
    {
        float length = 0.0f;
        float3 pos1 = f3zero;
        
        float3 pos2 = f3zero;
        float u = 0.0f;
                
        //[loop]
        while (u <= 1.0f)
        {
            pos1 = CurveGM(GM, u);
            u = u + delta;
            pos2 = CurveGM(GM, u);
            length = length + distance(pos1, pos2);
        }

        return length;
    }
    
    
    //
    static float3 Curve1(float3 p0, float3 p1, float3 dp0, float3 dp1, float u)
    {
        float4 U;
        float4x4 M;
        float4x3 G;
        float3 R;

		//matU
        U.x = u * u * u;
        U.y = u * u;
        U.z = u;
        U.w = 1.0f;

		//matM
        M._m00 = 2;
        M._m01 = -2;
        M._m02 = 1;
        M._m03 = 1;
        
        M._m10 = -3;
        M._m11 = 3;
        M._m12 = -2;
        M._m13 = -1;
        
        M._m20 = 0;
        M._m21 = 0;
        M._m22 = 1;
        M._m23 = 0;
        
        M._m30 = 1;
        M._m31 = 0;
        M._m32 = 0;
        M._m33 = 0;

		//matG
        G._m00 = p0.x;
        G._m01 = p0.y;
        G._m02 = p0.z;
        
        G._m10 = p1.x;
        G._m11 = p1.y;
        G._m12 = p1.z;
        
        G._m20 = dp0.x;
        G._m21 = dp0.y;
        G._m22 = dp0.z;
        
        G._m30 = dp1.x;
        G._m31 = dp1.y;
        G._m32 = dp1.z;

		//multiplication
        R = mul(mul(U, M), G);

        return R;
    }
    
};

struct Terrain
{
    static float SampleHMap(uint3 id, Texture2D tex, float hy)
    {
        float h;
    
        h = 0.25f * (
        tex[id.xz + uint2(0.0f, 0.0f)] +
        tex[id.xz + uint2(1.0f, 0.0f)] +
        tex[id.xz + uint2(0.0f, 1.0f)] +
        tex[id.xz + uint2(1.0f, 1.0f)]);
    
        h = hy * h;
    
        return h;
    }

    static float3 SampleHMapNormal(uint3 id, Texture2D tex, float3 tSize, float hy)
    {
        float3 n;
    
        float h[2][2];
        int i = 0;
        int j = 0;
        for (i = 0; i < 2; i++)
        {
            for (j = 0; j < 2; j++)
            {
                h[i][j] = hy * tex[id.xz + uint2(i, j)];
            }
        }
    
        float3 pos[2][2];
        float x0 = 0.0f;
        float z0 = 0.0f;
        float x1 = tSize.x;
        float z1 = tSize.z;
        pos[0][0] = float3(x0, h[0][0], z0);
        pos[0][1] = float3(x0, h[0][1], z1);
        pos[1][1] = float3(x1, h[1][1], z1);
        pos[1][0] = float3(x1, h[1][0], z0);
    
        float3 nom[2][2];
    
        nom[0][0] = normalize(cross((pos[0][1] - pos[0][0]), (pos[1][0] - pos[0][0])));
        nom[0][1] = normalize(cross((pos[1][0] - pos[0][1]), (pos[0][0] - pos[0][1])));
        nom[1][1] = normalize(cross((pos[1][0] - pos[1][1]), (pos[0][1] - pos[1][1])));
        nom[1][0] = normalize(cross((pos[0][0] - pos[1][0]), (pos[1][1] - pos[1][0])));
       
        n = normalize(0.25f * (nom[0][0] + nom[0][1] + nom[1][1] + nom[1][0]));
    
        return n;
    }
        
    static uint2 GetHMapPos(float3 _posW, float4x4 _T, float2 _t1_t0)
    {
        float2 posT;
    
        float3 temp = mul(_T, float4(_posW, 1.0f)).xyz;
        float2 p0 = temp.xz;
    
        posT = _t1_t0 * p0;
    
        return (uint2) posT;
    }

    static bool isOutNV(uint2 posT, Texture2D<float4> _alphaTex)
    {
        if (_alphaTex[posT].y != 0.0f)
        {
            return true;
        }
    
        return false;
    }
    
    static float4 GetTerrainArea(uint2 posT, Texture2D<float4> _alphaTex)
    {
        float4 area = _alphaTex[posT];
        
        //if (area.x == 1.0f)
        //{
        //    area = float4(1.0f, 0.0f, 0.0f, 0.0f);
        //}
        //else if (area.y == 1.0f)
        //{
        //    area = float4(0.0f, 1.0f, 0.0f, 0.0f);
        //}
        //else if (area.z == 1.0f)
        //{
        //    area = float4(0.0f, 0.0f, 1.0f, 0.0f);
        //}
        //else if (area.w == 1.0f)
        //{
        //    area = float4(0.0f, 0.0f, 0.0f, 1.0f);
        //}
        
        return area;
    }        

    static bool isOutNV_Hole(uint2 posT, Texture2D _holeTex)
    {
        if (_holeTex[posT].x == 0.0f)
        {
            return true;
        }
    
        return false;
    }

    static float4 GetNomH(uint2 posT, Texture3D<float4> nhTex)
    {
        return nhTex[uint3(posT.x, 0.0f, posT.y)];
    }        
};

struct Ray
{
    static bool intersectSphere(float3x2 ray, float4 sphere, out float2 ts, out float3x2 ps, out float minDist)
    {
        float3 x0 = ray.v3c0;
        float3 vd = normalize(ray.v3c1);

        float3 x1 = sphere.xyz;
        float r = sphere.w;

        float3 v0 = x1 - x0;

        float a = dot(vd, v0);

        ts = float2(0.0f, 0.0f);
        ps = float3x2(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        minDist = FLOAT_MAX;
                
        float a2 = a * a;
        float c2 = dot(v0, v0);
        float b2 = c2 - a2;

        float r2 = r * r;
        float d2 = r2 - b2;
        float e2 = a2 - d2;

        if (d2 < 0.0f)
        {
            return false;
        }

        float d = sqrt(d2);

        float t0 = a - d;
        float t1 = a + d;

        ts.x = t0;
        ts.y = t1;

        ps.v3c0 = x0 + t0 * vd;
        ps.v3c1 = x0 + t1 * vd;

        minDist = min(distance(x0, ps.v3c0), distance(x0, ps.v3c1));

        if (a < 0.0f)
        {
            return false;
        }
        if (e2 < 0.0f)
        {
            return false;
        }
        
        return true;
    }
};

#endif