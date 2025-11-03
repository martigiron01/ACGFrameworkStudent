#version 330 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;
in vec4 v_color;
in vec2 v_uv;

out vec4 FragColor;

uniform mat4 u_viewprojection;
uniform vec3 u_camera_position;
uniform mat4 u_model;
uniform vec4 u_color;
uniform float u_absorption_coefficient;

uniform vec3 u_background_color = vec3(0.1, 0.1, 0.1);

// Box volume boundaries in world space
uniform vec3 u_box_min = vec3(-0.5);
uniform vec3 u_box_max = vec3(0.5);

vec2 intersectAABB(vec3 rayOrigin, vec3 rayDir, vec3 boxMin, vec3 boxMax)
{
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return vec2(tNear, tFar);
}

void main()
{
    // 1. Initialize the ray
    vec3 rayOrigin = u_camera_position;
    vec3 rayDir = normalize(v_world_position - u_camera_position);

    // 2. Compute intersection with volume
    vec2 tHit = intersectAABB(rayOrigin, rayDir, u_box_min, u_box_max);
    float tNear = tHit.x;
    float tFar = tHit.y;

    // If no intersection, discard
    if (tFar < 0.0 || tNear > tFar)
        discard;

    // 3. Optical thickness
    float thickness = max(0.0, tFar - tNear);

    // 4. Transmittance (Beer–Lambert law)
    float transmittance = exp(-u_absorption_coefficient * thickness);

    // 5. Final pixel color
    vec3 baseColor = u_color.rgb;
    vec3 background = u_background_color;
    vec3 finalColor = mix(baseColor, background, transmittance);

    FragColor = vec4(finalColor, u_color.a);
}
