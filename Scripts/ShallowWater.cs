using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShallowWater : MonoBehaviour
{
    public const float gravity = 9.807f;

    public int xResolution, yResolution;
    public float additionalHeight;
    public bool isSimulating = false;
    public Material sideWaterMaterial;

    private Mesh mesh;
    private Mesh sidex0;
    private Mesh sidexY;
    private Mesh side0y;
    private Mesh sideXy;
    private Vector3[] vertices;
    private float[] heights;
    private Vector3[] velocities;
    private float deltaX;
    private float deltaY;

    private float[] advectedHeights;
    private float[] advectedV1;
    private float[] advectedV2;

    private float defaultHeight = 0.5f;

    // Start is called before the first frame update
    void Awake()
    {
        Init();
    }

    private void OnValidate()
    {
        Init();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Application.isPlaying && isSimulating)
        {
            Simulate();
        }
    }

    void Init()
    {
        if (xResolution <= 1 || yResolution <= 1)
            return;

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Water surface";

        int res = xResolution* yResolution;
        velocities = new Vector3[res];
        heights = new float[res];
        advectedHeights = new float[res];
        advectedV1 = new float[res];
        advectedV2 = new float[res];
        vertices = new Vector3[res];
        deltaX = 1.0f / (xResolution - 1);
        deltaY = 1.0f / (yResolution - 1);

        for (int i = 0; i < res; i++)
        {
            heights[i] = defaultHeight;
        }

        heights[at(0, 0)] = additionalHeight;
        heights[at(0, 1)] = additionalHeight;
        heights[at(1, 0)] = additionalHeight;

        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0, y = 0; y < yResolution; y++)
        {
            for (int x = 0; x < xResolution; x++, i++)
            {
                vertices[i] = new Vector3((float)x / (xResolution - 1) - 0.5f, heights[i], (float)y / (yResolution - 1) - 0.5f);
                uv[i] = new Vector2((float)x / xResolution, (float)y / yResolution);
            }
        }
        mesh.vertices = vertices;
        //ApplyVertices();
        //InitBounds();
        //mesh.uv = uv;

        // 三角形
        /*
        int[] triangles = new int[(xResolution - 1) * (yResolution - 1) * 6];
        for (int ti = 0, vi = 0, y = 0; y < yResolution - 1; y++, vi++)
        {
            for (int x = 0; x < xResolution - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xResolution;
                triangles[ti + 5] = vi + xResolution + 1;
            }
        }
        mesh.triangles = triangles;*/
        SetupGridTriangles(mesh, xResolution, yResolution);
        mesh.RecalculateNormals();

        CreateSides();
    }

    void CreateSides()
    {
        if(transform.Find("Side x0") == null)
        {
            /*GameObject gsidex0 = new GameObject();
            GameObject gsidexY = new GameObject();
            GameObject gside0y = new GameObject();
            GameObject gsideXy = new GameObject();
            gsidex0.name = "Side x0";
            gsidexY.name = "Side xY";
            gside0y.name = "Side 0y";
            gsideXy.name = "Side Xy";

            gsidex0.GetComponent<MeshFilter>().mesh = sidex0 = new Mesh();
            gsidex0.name = "Side x0";
            gsidexY.GetComponent<MeshFilter>().mesh = sidexY = new Mesh();
            gsidexY.name = "Side xY";
            gside0y.GetComponent<MeshFilter>().mesh = side0y = new Mesh();
            gside0y.name = "Side 0y";
            gsideXy.GetComponent<MeshFilter>().mesh = sideXy = new Mesh();
            gsideXy.name = "Side Xy";

            Vector3[] verticesx0 = new Vector3[2 * xResolution];
            Vector3[] verticesxY = new Vector3[2 * xResolution];
            Vector3[] vertices0y = new Vector3[2 * yResolution];
            Vector3[] verticesXy = new Vector3[2 * yResolution];

            for(int x = 0; x < xResolution; x++)
            {
                Vector3 pos0 = vertices[at(x, 0)];
                Vector3 posY = vertices[at(x, yResolution - 1)];
                verticesx0[x] = new Vector3(pos0.x, 0, pos0.z);
                verticesx0[x + xResolution] = new Vector3(pos0.x, defaultHeight, pos0.z);
                verticesxY[x] = new Vector3(posY.x, 0, posY.z);
                verticesxY[x + xResolution] = new Vector3(posY.x, defaultHeight, posY.z);
            }
            sidex0.vertices = verticesx0;
            SetupGridTriangles(sidex0, xResolution, 2);
            sidex0.RecalculateNormals();*/

            sidex0 = CreateSide("x0", xResolution);
            sidexY = CreateSide("xY", xResolution);
            side0y = CreateSide("0y", yResolution);
            sideXy = CreateSide("Xy", yResolution);

            Vector3[] verticesx0 = sidex0.vertices;
            Vector3[] verticesxY = sidexY.vertices;
            Vector3[] vertices0y = side0y.vertices;
            Vector3[] verticesXy = sideXy.vertices;

            for (int x = 0; x < xResolution; x++)
            {
                Vector3 pos0 = vertices[at(x, 0)];
                Vector3 posY = vertices[at(x, yResolution - 1)];
                verticesx0[x] = new Vector3(pos0.x, 0, pos0.z);
                verticesxY[x] = new Vector3(posY.x, 0, posY.z);
            }
            for (int y = 0; y < yResolution; y++)
            {
                Vector3 pos0 = vertices[at(0, y)];
                Vector3 posX = vertices[at(xResolution - 1, y)];
                vertices0y[y] = new Vector3(pos0.x, 0, pos0.z);
                verticesXy[y] = new Vector3(posX.x, 0, posX.z);
            }
            sidex0.vertices = verticesx0;
            sidexY.vertices = verticesxY;
            side0y.vertices = vertices0y;
            sideXy.vertices = verticesXy;
            UpdateSides();
        } else
        {
            sidex0 = transform.Find("Side x0").gameObject.GetComponent<MeshFilter>().sharedMesh;
            sidexY = transform.Find("Side xY").gameObject.GetComponent<MeshFilter>().sharedMesh;
            side0y = transform.Find("Side 0y").gameObject.GetComponent<MeshFilter>().sharedMesh;
            sideXy = transform.Find("Side Xy").gameObject.GetComponent<MeshFilter>().sharedMesh;
            UpdateSides();
        }
    }

    Mesh CreateSide(string name, int resolution)
    {
        Mesh sideMesh;
        GameObject side = new GameObject();
        side.AddComponent<MeshFilter>();
        side.AddComponent<MeshRenderer>();
        side.transform.parent = transform;
        side.name = "Side " + name;
        side.GetComponent<MeshFilter>().mesh = sideMesh = new Mesh();
        side.name = "Side " + name;
        side.GetComponent<MeshRenderer>().material = sideWaterMaterial;

        Vector3[] sideVertices = new Vector3[2 * resolution];
        sideMesh.vertices = sideVertices;
        SetupGridTriangles(sideMesh, resolution, 2);
        sideMesh.RecalculateNormals();

        return sideMesh;
    }

    void Simulate()
    {
        float[] v1 = new float[mesh.vertexCount];
        float[] v2 = new float[mesh.vertexCount];
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            v1[i] = velocities[i].x;
            v2[i] = velocities[i].z;
        }
        Advect(heights, advectedHeights);
        Advect(v1, advectedV1);
        Advect(v2, advectedV2);

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            velocities[i] = new Vector3(v1[i], velocities[i].z, v2[i]);
            heights[i] = advectedHeights[i];
        }

        UpdateHeight();
        UpdateVelocities();

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 v = vertices[i];
            vertices[i] = new Vector3(v.x, heights[i], v.z);
        }
        //ApplyVertices();
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        UpdateSides();
    }

    void Advect(float[] field, float[] advectedField)
    {
        for (int i = 0, y = 0; y < yResolution; y++)
        {
            for (int x = 0; x < xResolution; x++, i++)
            {
                Vector3 backPos = vertices[i] - Time.fixedDeltaTime*velocities[i];
                advectedField[i] = Interpolate(backPos, field);
            }
        }
    }

    float Interpolate(Vector3 pos, float[] field)
    {
        pos += new Vector3(0.5f, 0, 0.5f);
        float x = pos.x / deltaX;
        float y = pos.z / deltaY;
        int xDisc = Mathf.RoundToInt(x);
        int yDisc = Mathf.RoundToInt(y);
        float xRest = x - xDisc;
        float yRest = y - yDisc;

        return Interpolate(xDisc, yDisc, xRest, yRest, field);
    }

    float Interpolate(int x, int y, float u, float v, float[] field)
    {
        if(x >= xResolution - 1)
        {
            x = xResolution - 2;
            u = 1;
        }

        if (y >= yResolution - 1)
        {
            y = yResolution - 2;
            v = 1;
        }

        float x1Lerp = Mathf.Lerp(field[at(x, y)], field[at(x + 1, y)], u);
        float x2Lerp = Mathf.Lerp(field[at(x, y + 1)], field[at(x + 1, y + 1)], u);
        float yLerp = Mathf.Lerp(x1Lerp, x2Lerp, v);
        return yLerp;
    }

    void UpdateHeight()
    {
        for (int y = 0; y < yResolution - 1; y++)
        {
            for (int x = 0; x < xResolution - 1; x++)
            {
                heights[at(x, y)] -= heights[at(x, y)] * (
                    (velocities[at(x + 1, y)].x - velocities[at(x, y)].x) / deltaX +
                    (velocities[at(x, y + 1)].z - velocities[at(x, y)].z) / deltaY
                ) * Time.fixedDeltaTime;
            }
        }
    }

    void UpdateVelocities()
    {
        for (int y = 0; y < yResolution; y++)
        {
            for (int x = 1; x < xResolution; x++)
            {
                Vector3 v = velocities[at(x, y)];
                velocities[at(x, y)] = new Vector3(
                    v.x + gravity * Time.fixedDeltaTime * (heights[at(x-1, y)] - heights[at(x, y)]) / deltaX ,
                    v.y, v.z
                );
            }
        }

        for (int y = 1; y < yResolution; y++)
        {
            for (int x = 0; x < xResolution; x++)
            {
                Vector3 v = velocities[at(x, y)];
                velocities[at(x, y)] = new Vector3(
                    v.x,
                    v.y,
                    v.z + gravity * Time.fixedDeltaTime * (heights[at(x, y - 1)] - heights[at(x, y)]) / deltaY
                );
            }
        }
    }

    void SetupGridTriangles(Mesh amesh, int xRes, int yRes)
    {
        int[] triangles = new int[(xRes - 1) * (yRes - 1) * 6];
        for (int ti = 0, vi = 0, y = 0; y < yRes - 1; y++, vi++)
        {
            for (int x = 0; x < xRes - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xRes;
                triangles[ti + 5] = vi + xRes + 1;
            }
        }
        amesh.triangles = triangles;
    }

    void UpdateSides()
    {
        Vector3[] verticesx0 = sidex0.vertices;
        Vector3[] verticesxY = sidexY.vertices;
        Vector3[] vertices0y = side0y.vertices;
        Vector3[] verticesXy = sideXy.vertices;
        for (int x = 0; x < xResolution; x++)
        {
            Vector3 pos0 = vertices[at(x, 0)];
            Vector3 posY = vertices[at(x, yResolution - 1)];
            verticesx0[x + xResolution] = new Vector3(pos0.x, heights[at(x, 0)], pos0.z);
            verticesxY[x + xResolution] = new Vector3(posY.x, heights[at(x, yResolution - 1)], posY.z);
        }
        for (int y = 0; y < yResolution; y++)
        {
            Vector3 pos0 = vertices[at(0, y)];
            Vector3 posX = vertices[at(xResolution - 1, y)];
            vertices0y[y + yResolution] = new Vector3(pos0.x, heights[at(0, y)], pos0.z);
            verticesXy[y + yResolution] = new Vector3(posX.x, heights[at(xResolution - 1, y)], posX.z);
        }
        sidex0.vertices = verticesx0;
        sidexY.vertices = verticesxY;
        side0y.vertices = vertices0y;
        sideXy.vertices = verticesXy;
    }

    void ApplyVertices()
    {
        Vector3[] meshVerts = mesh.vertices;
        for (int i = 0, y = 0; y < yResolution; y++)
        {
            for (int x = 0; x < xResolution; x++, i++)
            {
                meshVerts[at(x + 1, y + 1, xResolution + 2)] = vertices[i];
            }
        }
        mesh.vertices = meshVerts;
    }

    void InitBounds()
    {
        Vector3[] meshVerts = mesh.vertices;

        // 边界
        for (int y = 0; y < yResolution; y++)
        {
            Vector3 pos1 = vertices[at(0, y)];
            Vector3 pos2 = vertices[at(xResolution - 1, y)];
            meshVerts[at(0, y, xResolution + 2)] = new Vector3(pos1.x, 0, pos1.z);
            meshVerts[at(xResolution + 1, y, xResolution + 2)] = new Vector3(pos2.x, 0, pos2.z);
        }
        for (int x = 0; x < xResolution; x++)
        {
            Vector3 pos1 = vertices[at(x, 0)];
            Vector3 pos2 = vertices[at(x, yResolution - 1)];
            meshVerts[at(x, 0, yResolution + 2)] = new Vector3(pos1.x, 0, pos1.z);
            meshVerts[at(0, yResolution + 1, xResolution + 2)] = new Vector3(pos2.x, 0, pos2.z);
        }

        mesh.vertices = meshVerts;
    }

    int at(int x, int y)
    {
        return y * xResolution + x;
    }

    int at(int x, int y, int xRel)
    {
        return y * xRel + x;
    }
}
