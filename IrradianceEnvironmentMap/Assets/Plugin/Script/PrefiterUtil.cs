// PrefilterUtil.cs
//
// Author: i_dovelemon[1322600812@qq.com], 2020-1-1
//
// Prefilter diffuse environment
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefilterUtil
{
    #region SH
    // Prefilter environment map to compute 9 SH coeffcient
    //
    // env: 
    // Environment map data, must stored by +x,-x,+y,-y,+z,-z order.
    // And +z is up vector.
    //
    // size: Environment map size in pixels
    //
    // return: Result stored in Y00, Y1-1, Y10, Y11, Y2-2, Y2-1, Y20, Y21, Y22 order
    //
    public static List<Color> PrefilterSH(List<Color[]> env, int size)
    {
        List<Color> result = new List<Color>();

        int[] ls = {0, 1, 1, 1, 2, 2, 2, 2 ,2 };
        int[] ms = {0, 1, 0, -1, 2, 1, 0, -1, -2 };

        float halfSize = size / 2.0f;
        for (int i = 0; i < 9; i++)
        {
            Color coeffcient = new Color(0.0f, 0.0f, 0.0f);
            float totalTexelSolidAngle = 0.0f;

            // +x
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 pos = new Vector3(halfSize - 0.5f, halfSize - x - 0.5f, halfSize - y - 0.5f);
                    pos.Normalize();

                    Color radiance = env[0][y * size + x];
                    float sh = SHFunction(pos, ls[i], ms[i]);
                    float texelSolidAngle = TexelCoordSolidAngle(x, y, size);
                    coeffcient = coeffcient + radiance * sh * texelSolidAngle;

                    totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                }
            }

            // -x
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 pos = new Vector3(-halfSize + 0.5f, x - halfSize + 0.5f, halfSize - y - 0.5f);
                    pos.Normalize();

                    Color radiance = env[1][y * size + x];
                    float sh = SHFunction(pos, ls[i], ms[i]);
                    float texelSolidAngle = TexelCoordSolidAngle(x, y, size);
                    coeffcient = coeffcient + radiance * sh * texelSolidAngle;

                    totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                }
            }

            // +y
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 pos = new Vector3(-halfSize + 0.5f + x, halfSize - 0.5f, halfSize - y - 0.5f);
                    pos.Normalize();

                    Color radiance = env[2][y * size + x];
                    float sh = SHFunction(pos, ls[i], ms[i]);
                    float texelSolidAngle = TexelCoordSolidAngle(x, y, size);
                    coeffcient = coeffcient + radiance * sh * texelSolidAngle;

                    totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                }
            }

            // -y
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 pos = new Vector3(halfSize - 0.5f - x, -halfSize + 0.5f, halfSize - y - 0.5f);
                    pos.Normalize();

                    Color radiance = env[3][y * size + x];
                    float sh = SHFunction(pos, ls[i], ms[i]);
                    float texelSolidAngle = TexelCoordSolidAngle(x, y, size);
                    coeffcient = coeffcient + radiance * sh * texelSolidAngle;

                    totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                }
            }

            // +z
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 pos = new Vector3(-halfSize + 0.5f + x, -halfSize + 0.5f + y, halfSize - 0.5f);
                    pos.Normalize();

                    Color radiance = env[4][y * size + x];
                    float sh = SHFunction(pos, ls[i], ms[i]);
                    float texelSolidAngle = TexelCoordSolidAngle(x, y, size);
                    coeffcient = coeffcient + radiance * sh * texelSolidAngle;

                    totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                }
            }

            // -z
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 pos = new Vector3(-halfSize + 0.5f + x, halfSize - 0.5f - y, -halfSize + 0.5f);
                    pos.Normalize();

                    Color radiance = env[5][y * size + x];
                    float sh = SHFunction(pos, ls[i], ms[i]);
                    float texelSolidAngle = TexelCoordSolidAngle(x, y, size);
                    coeffcient = coeffcient + radiance * sh * texelSolidAngle;

                    totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                }
            }

            // Error correction
            coeffcient = coeffcient * 4.0f * Mathf.PI / totalTexelSolidAngle;

            result.Add(coeffcient);
        }

        return result;
    }

    private static float SHFunction(Vector3 v, int l, int m)
    {
        if (!(0 <= l && l <= 2 && -2 <= m && m <= 2)) return 0.0f;

        if (l == 0 && m == 0)
        {
            // Y00
            return 0.282095f;
        }
        else if (l == 1)
        {
            if (m == 0)
            {
                // Y10
                return 0.488603f * v.z;
            }
            else if (m == -1)
            {
                // Y1-1
                return 0.488603f * v.y;
            }
            else if (m == 1)
            {
                // Y11
                return 0.488603f * v.x;
            }
        }
        else if (l == 2)
        {
            if (m == 0)
            {
                // Y20
                return 0.315392f * (3.0f * v.z * v.z - 1.0f);
            }
            else if (m == -1)
            {
                // Y2-1
                return 1.092548f * v.y * v.z;
            }
            else if (m == 1)
            {
                // Y21
                return 1.092548f * v.x * v.z;
            }
            else if (m == -2)
            {
                // Y2-2
                return 1.092548f * v.x * v.y;
            }
            else if (m == 2)
            {
                // Y22
                return 0.546274f * (v.x * v.x - v.y * v.y);
            }
        }

        return 0.0f;
    }
    #endregion

    #region BruteForce
    // Prefilter environment map to compute irradiance map with brute force way
    //
    // env: 
    // Environment map data, must stored by +x,-x,+y,-y,+z,-z order.
    // And +z is up vector.
    //
    // srcSize: Environment map size in pixels
    //
    // dstSize: Irradiance map size in pixels
    //
    // return: Result environment map stored by +x,-x,+y,-y,+z,-z order
    //
    public static List<Color[]> PrefilterBruteForce(List<Color[]> env, int srcSize, int dstSize)
    {
        List<Color[]> result = new List<Color[]>();
        for (int i = 0; i < 6; i++)
        {
            result.Add(new Color[dstSize * dstSize]);
        }

        float srcHalfSize = srcSize / 2.0f;
        float dstHalfSize = dstSize / 2.0f;

        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            for (int j = 0; j < dstSize; j++)
            {
                for (int i = 0; i < dstSize; i++)
                {
                    // Calculate target irradiance map direction
                    Vector3 dir = new Vector3(0.0f, 0.0f, 0.0f);
                    if (faceIndex == 0)  // +x
                    {
                        dir = new Vector3(dstHalfSize - 0.5f, dstHalfSize - i - 0.5f, dstHalfSize - j - 0.5f);
                    }
                    else if (faceIndex == 1)  // -x
                    {
                        dir = new Vector3(-dstHalfSize + 0.5f, i - dstHalfSize + 0.5f, dstHalfSize - j - 0.5f);
                    }
                    else if (faceIndex == 2)  // +y
                    {
                        dir = new Vector3(-dstHalfSize + 0.5f + i, dstHalfSize - 0.5f, dstHalfSize - j - 0.5f);
                    }
                    else if (faceIndex == 3)  // -y
                    {
                        dir = new Vector3(dstHalfSize - 0.5f - i, -dstHalfSize + 0.5f, dstHalfSize - j - 0.5f);
                    }
                    else if (faceIndex == 4)  // +z
                    {
                        dir = new Vector3(-dstHalfSize + 0.5f + i, -dstHalfSize + 0.5f + j, dstHalfSize - 0.5f);
                    }
                    else if (faceIndex == 5)  // -z
                    {
                        dir = new Vector3(-dstHalfSize + 0.5f + i, dstHalfSize - 0.5f - j, -dstHalfSize + 0.5f);
                    }
                    dir.Normalize();

                    float totalTexelSolidAngle = 0.0f;
                    Color irradiance = new Color(0.0f, 0.0f, 0.0f);

                    // +x
                    for (int y = 0; y < srcSize; y++)
                    {
                        for (int x = 0; x < srcSize; x++)
                        {
                            Vector3 pos = new Vector3(srcHalfSize - 0.5f, srcHalfSize - x - 0.5f, srcHalfSize - y - 0.5f);
                            pos.Normalize();

                            Color radiance = env[0][y * srcSize + x];
                            float ndotl = Mathf.Max(0.0f, Vector3.Dot(pos, dir));
                            float texelSolidAngle = TexelCoordSolidAngle(x, y, srcSize);
                            irradiance = irradiance + radiance * ndotl * texelSolidAngle;
                            totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                        }
                    }

                    // -x
                    for (int y = 0; y < srcSize; y++)
                    {
                        for (int x = 0; x < srcSize; x++)
                        {
                            Vector3 pos = new Vector3(-srcHalfSize + 0.5f, x - srcHalfSize + 0.5f, srcHalfSize - y - 0.5f);
                            pos.Normalize();

                            Color radiance = env[1][y * srcSize + x];
                            float ndotl = Mathf.Max(0.0f, Vector3.Dot(pos, dir));
                            float texelSolidAngle = TexelCoordSolidAngle(x, y, srcSize);
                            irradiance = irradiance + radiance * ndotl * texelSolidAngle;
                            totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                        }
                    }

                    // +y
                    for (int y = 0; y < srcSize; y++)
                    {
                        for (int x = 0; x < srcSize; x++)
                        {
                            Vector3 pos = new Vector3(-srcHalfSize + 0.5f + x, srcHalfSize - 0.5f, srcHalfSize - y - 0.5f);
                            pos.Normalize();

                            Color radiance = env[2][y * srcSize + x];
                            float ndotl = Mathf.Max(0.0f, Vector3.Dot(pos, dir));
                            float texelSolidAngle = TexelCoordSolidAngle(x, y, srcSize);
                            irradiance = irradiance + radiance * ndotl * texelSolidAngle;
                            totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                        }
                    }

                    // -y
                    for (int y = 0; y < srcSize; y++)
                    {
                        for (int x = 0; x < srcSize; x++)
                        {
                            Vector3 pos = new Vector3(srcHalfSize - 0.5f - x, -srcHalfSize + 0.5f, srcHalfSize - y - 0.5f);
                            pos.Normalize();

                            Color radiance = env[3][y * srcSize + x];
                            float ndotl = Mathf.Max(0.0f, Vector3.Dot(pos, dir));
                            float texelSolidAngle = TexelCoordSolidAngle(x, y, srcSize);
                            irradiance = irradiance + radiance * ndotl * texelSolidAngle;
                            totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                        }
                    }

                    // +z
                    for (int y = 0; y < srcSize; y++)
                    {
                        for (int x = 0; x < srcSize; x++)
                        {
                            Vector3 pos = new Vector3(-srcHalfSize + 0.5f + x, -srcHalfSize + 0.5f + y, srcHalfSize - 0.5f);
                            pos.Normalize();

                            Color radiance = env[4][y * srcSize + x];
                            float ndotl = Mathf.Max(0.0f, Vector3.Dot(pos, dir));
                            float texelSolidAngle = TexelCoordSolidAngle(x, y, srcSize);
                            irradiance = irradiance + radiance * ndotl * texelSolidAngle;
                            totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                        }
                    }

                    // -z
                    for (int y = 0; y < srcSize; y++)
                    {
                        for (int x = 0; x < srcSize; x++)
                        {
                            Vector3 pos = new Vector3(-srcHalfSize + 0.5f + x, srcHalfSize - 0.5f - y, -srcHalfSize + 0.5f);
                            pos.Normalize();

                            Color radiance = env[5][y * srcSize + x];
                            float ndotl = Mathf.Max(0.0f, Vector3.Dot(pos, dir));
                            float texelSolidAngle = TexelCoordSolidAngle(x, y, srcSize);
                            irradiance = irradiance + radiance * ndotl * texelSolidAngle;
                            totalTexelSolidAngle = totalTexelSolidAngle + texelSolidAngle;
                        }
                    }

                    // Error correction
                    irradiance = irradiance * 4 * Mathf.PI / totalTexelSolidAngle;

                    // Transform into radiance
                    irradiance = irradiance / Mathf.PI;

                    result[faceIndex][j * dstSize + i] = irradiance;
                }
            }
        }

        return result;
    }
    #endregion

    private static float AreaElement(float x, float y)
    {
        return Mathf.Atan2(x * y, Mathf.Sqrt(x * x + y * y + 1.0f));
    }

    private static float TexelCoordSolidAngle(float x, float y, int size)
    {
        // Scale up to [-1,1] range (inclusive), offset by 0.5 to point to texel center
        float u = 2.0f * (x + 0.5f) / size - 1.0f;
        float v = 2.0f * (y + 0.5f) / size - 1.0f;

        float invRes = 1.0f / size;

        // Project area for this texel
        float x0 = u - invRes;
        float y0 = v - invRes;
        float x1 = u + invRes;
        float y1 = v + invRes;
        return AreaElement(x0, y0) - AreaElement(x0, y1) - AreaElement(x1, y0) + AreaElement(x1, y1);
    }
}
