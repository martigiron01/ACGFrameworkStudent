#version 330 core

in vec3 v_world_position;
in vec3 v_normal;
in vec4 v_color;
in vec2 v_uv;

out vec4 FragColor;

uniform vec3 u_camera_position;
uniform vec4 u_color;
uniform float u_absorption_coefficient;
uniform mat4 u_model;

uniform vec3 u_box_min = vec3(-0.5);
uniform vec3 u_box_max = vec3(0.5);
uniform vec3 u_background_color = vec3(0.1, 0.1, 0.1);

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
    // Initialize ray in world space
    vec3 rayOrigin = u_camera_position;
    vec3 rayDir = normalize(v_world_position - u_camera_position);

    // Transform ray into object space
    mat4 invModel = inverse(u_model);
    vec3 rayOriginObj = (invModel * vec4(rayOrigin, 1.0)).xyz;
    vec3 rayDirObj = normalize((invModel * vec4(rayDir, 0.0)).xyz);

    // Compute intersection with box in object space
    vec2 intersection = intersectAABB(rayOriginObj, rayDirObj, u_box_min, u_box_max);
    float tEntry = intersection.x;
    float tExit = intersection.y;

    // If no intersection, discard fragment
    if (tExit < 0.0 || tEntry > tExit)
        discard;

    // Optical thickness
    float thickness = max(0.0, tExit - tEntry);

    // Transmittance
    float transmittance = exp(- thickness * u_absorption_coefficient);

    // Final color
    vec3 baseColor = u_color.rgb;
    vec3 background = u_background_color;
    vec3 finalColor = mix(baseColor, background, transmittance);

    FragColor = vec4(finalColor, u_color.a);
}
