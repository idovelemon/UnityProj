//----------------------------------------------------------------------------
// Intersection regin
//----------------------------------------------------------------------------
bool RayIntersectSphere(
    float3 ro, float3 rd, // Ray
    float3 so, float sr,  // Sphere
    out float ra, out float rb  // Result
    )
{
    ra = 0.0f;
    rb = 0.0f;
    float a = dot(rd, rd);
    float b = 2.0f * dot(rd, ro);
    float c = dot(ro, ro) - (sr * sr);
    float d = (b * b) - 4.0f * a * c;
    if (d < 0.0f) return false;

    ra = max(0.0f, (-b - sqrt(d)) / 2.0f * a);  // Fuck here, ra can not be negative
    rb = (-b + sqrt(d)) / 2.0f * a;
    if (ra > rb) return false;  // Fuck here, rb must be bigger than ra
    return true;
}

bool IntersectAtmosphere(
    float3 ro, float3 rd, // Ray
    float3 o, float ar, float pr, // Planet and atmosphere
    out float a, out float b // Result
)
{
    if (!RayIntersectSphere(ro, rd, o, ar, a, b)) return false;

    float pa = 0.0f, pb = 0.0f;
    if (RayIntersectSphere(ro, rd, o, pr, pa, pb))
    {
        b = pa;
    }

    return true;
}

//----------------------------------------------------------------------------
// Rayleigh Scattering regin
//----------------------------------------------------------------------------
float RayleighDensityRatio(float h)
{
    float H = 8e3;
    return exp(-h / H);
}

float3 RayleighScatteringCoefficientAtSealevel()
{
    return float3(0.00000519673f, 0.0000121427f, 0.0000296453f);
}

float RayleighScatteringPhase(float theta)
{
    return 3.0f * (1 + theta * theta) / (16.0f * 3.1415926f);
}

float RayleighOpticalDepthViewRay(float3 viewPos, float3 viewDir,  // View ray
                            float ta, float tp,  // Position
                            uint sampleN,  // Sample
                            float3 planetPos, float planentRadius  // Planent
    )
{
    // Split intersect ray into N segment
    float ds = (tp - ta) / sampleN;
    float st = ta;

    float opticalDepth = 0.0f;
    for (uint i = 0u; i < sampleN; i++)
    {
        // Current sample position
        float3 pos = viewPos + viewDir * (st + ds * 0.5f);

        // Current sample height
        float height = distance(pos, planetPos) - planentRadius;

        opticalDepth = opticalDepth + RayleighDensityRatio(height) * ds;

        st = st + ds;
    }

    return opticalDepth;
}

bool RayleighOpticalDepthLightRay(
    float3 p, float3 sunDir,  // Light ray
    float3 planetPos, float planentRadius, float atmosphereRadius,  // Planent and Atmosphere
    uint sampleN,  // Sample
    out float opticalDepth
    )
{
    float ta = 0.0f, tb = 0.0f;
    RayIntersectSphere(p, sunDir, planetPos, atmosphereRadius, ta, tb);

    float ds = tb / sampleN;
    float st = 0.0f;

    opticalDepth = 0.0f;
    for (uint i = 0; i < sampleN; i++)
    {
        // Current sample position
        float3 pos = p + sunDir * (st + ds * 0.5f);

        // Current sample height
        float height = distance(pos, planetPos) - planentRadius;
        if (height < 0.0f) return false;

        opticalDepth = opticalDepth + RayleighDensityRatio(height) * ds;

        st = st + ds;
    }

    return true;
}

float3 RayleighLightContributionIntegration(float h, float ds, float tp, float ta,  // Position
                                    float3 viewPos, float3 viewDir,  // View ray
                                    float3 sunDir,  // Sun direction
                                    uint sampleN,  // Sample
                                    float3 planetPos, float planentRadius, float atmosphericRadius // Planent
                                    )
{
    float lightRayDepth = 0.0f;
    float3 position = viewPos + viewDir * tp;
    if (!RayleighOpticalDepthLightRay(position, sunDir, planetPos, planentRadius, atmosphericRadius, sampleN, lightRayDepth))
    {
        // Occlussion by earth
        return float3(0.0f, 0.0f, 0.0f);
    }

    float viewRayDepth = RayleighOpticalDepthViewRay(viewPos, viewDir, ta, tp, sampleN, planetPos, planentRadius);
    float ratio = RayleighDensityRatio(h);
    return exp(-RayleighScatteringCoefficientAtSealevel() * (viewRayDepth + lightRayDepth)) * ratio * ds;
}

float3 RayleighAtmosphericScatteringIntegration(
    float3 viewPos, float3 viewDir,  // View
    float3 planetPos, float atmosphereRadius, float planetRadius,  // Planet and Atmosphere
    uint viewRaySampleN, uint lightRaySampleN,  // View and Light Ray sample time
    float3 sunIntensity,  float3 sunDir // Sun
    )
{
    float la = 0.0f, lb = 0.0f;
    bool isViewAtmosphere = IntersectAtmosphere(viewPos, viewDir, planetPos, atmosphereRadius, planetRadius, la, lb);
    if (!isViewAtmosphere)
    {
        // Do not view atmoshpere, there is not scattering happen
        return float3(0.0f, 0.0f, 0.0f);
    }

    // Split intersect ray into N segment
    float ds = (lb - la) / viewRaySampleN;
    float st = la;

    float3 totalContribution = 0.0f;
    for (uint i = 0u; i < viewRaySampleN; i++)
    {
        // Current sample position
        float tp = (st + ds * 0.5f);
        float3 pos = viewPos + viewDir * tp;

        float height = distance(pos, planetPos) - planetRadius;
        totalContribution = totalContribution + RayleighLightContributionIntegration(
            height, ds, tp, st, viewPos, viewDir, sunDir, lightRaySampleN, planetPos, planetRadius, atmosphereRadius);

        st = st + ds;
    }

    float3 coefficient = RayleighScatteringCoefficientAtSealevel();
    float phase = RayleighScatteringPhase(dot(viewDir, sunDir));
    return sunIntensity * coefficient * totalContribution * phase;
}

//----------------------------------------------------------------------------
// Mie Scattering regin
//----------------------------------------------------------------------------
float MieDensityRatio(float h)
{
    float H = 1200;
    return exp(-h / H);
}

float3 MieScatteringCoefficientAtSealevel()
{
    return float3(21e-6f, 21e-6f, 21e-6f);
}

float MieScatteringPhase(float theta)
{
    const float g = 0.99f;
    const float g2 = g * g;
    const float one_minus_g2 = 1.0f - g2;
    const float one_add_g2 = 1.0f + g2;
    const float two_add_g2 = 2.0f + g2;
    float a = 3.0f * one_minus_g2 * (1.0f + theta * theta);
    float b = 8.0f * 3.1415926f * two_add_g2 * pow(one_add_g2 - 2.0f * g * theta, 3.0f / 2.0f);
    return a / b;
}

float MieOpticalDepthViewRay(float3 viewPos, float3 viewDir,  // View ray
    float ta, float tp,  // Position
    uint sampleN,  // Sample
    float3 planetPos, float planentRadius  // Planent
)
{
    // Split intersect ray into N segment
    float ds = (tp - ta) / sampleN;
    float st = ta;

    float opticalDepth = 0.0f;
    for (uint i = 0u; i < sampleN; i++)
    {
        // Current sample position
        float3 pos = viewPos + viewDir * (st + ds * 0.5f);

        // Current sample height
        float height = distance(pos, planetPos) - planentRadius;

        opticalDepth = opticalDepth + MieDensityRatio(height) * ds;

        st = st + ds;
    }

    return opticalDepth;
}

bool MieOpticalDepthLightRay(
    float3 p, float3 sunDir,  // Light ray
    float3 planetPos, float planentRadius, float atmosphereRadius,  // Planent and Atmosphere
    uint sampleN,  // Sample
    out float opticalDepth
)
{
    float ta = 0.0f, tb = 0.0f;
    RayIntersectSphere(p, sunDir, planetPos, atmosphereRadius, ta, tb);

    float ds = tb / sampleN;
    float st = 0.0f;

    opticalDepth = 0.0f;
    for (uint i = 0; i < sampleN; i++)
    {
        // Current sample position
        float3 pos = p + sunDir * (st + ds * 0.5f);

        // Current sample height
        float height = distance(pos, planetPos) - planentRadius;
        if (height < 0.0f) return false;

        opticalDepth = opticalDepth + MieDensityRatio(height) * ds;

        st = st + ds;
    }

    return true;
}

float3 MieLightContributionIntegration(float h, float ds, float tp, float ta,  // Position
    float3 viewPos, float3 viewDir,  // View ray
    float3 sunDir,  // Sun direction
    uint sampleN,  // Sample
    float3 planetPos, float planentRadius, float atmosphericRadius // Planent
)
{
    float lightRayDepth = 0.0f;
    float3 position = viewPos + viewDir * tp;
    if (!MieOpticalDepthLightRay(position, sunDir, planetPos, planentRadius, atmosphericRadius, sampleN, lightRayDepth))
    {
        // Occlussion by earth
        return float3(0.0f, 0.0f, 0.0f);
    }

    float viewRayDepth = MieOpticalDepthViewRay(viewPos, viewDir, ta, tp, sampleN, planetPos, planentRadius);
    float ratio = MieDensityRatio(h);
    return exp(-MieScatteringCoefficientAtSealevel() * (viewRayDepth + lightRayDepth)) * ratio * ds;
}

float3 MieAtmosphericScatteringIntegration(
    float3 viewPos, float3 viewDir,  // View
    float3 planetPos, float atmosphereRadius, float planetRadius,  // Planet and Atmosphere
    uint viewRaySampleN, uint lightRaySampleN,  // View and Light Ray sample time
    float3 sunIntensity, float3 sunDir // Sun
)
{
    float la = 0.0f, lb = 0.0f;
    bool isViewAtmosphere = IntersectAtmosphere(viewPos, viewDir, planetPos, atmosphereRadius, planetRadius, la, lb);
    if (!isViewAtmosphere)
    {
        // Do not view atmoshpere, there is not scattering happen
        return float3(0.0f, 0.0f, 0.0f);
    }

    // Split intersect ray into N segment
    float ds = (lb - la) / viewRaySampleN;
    float st = la;

    float3 totalContribution = 0.0f;
    for (uint i = 0u; i < viewRaySampleN; i++)
    {
        // Current sample position
        float tp = (st + ds * 0.5f);
        float3 pos = viewPos + viewDir * tp;

        float height = distance(pos, planetPos) - planetRadius;
        totalContribution = totalContribution + MieLightContributionIntegration(
            height, ds, tp, st, viewPos, viewDir, sunDir, lightRaySampleN, planetPos, planetRadius, atmosphereRadius);

        st = st + ds;
    }

    float3 coefficient = MieScatteringCoefficientAtSealevel();
    float phase = MieScatteringPhase(dot(viewDir, sunDir));
    return sunIntensity * coefficient * totalContribution * phase;
}