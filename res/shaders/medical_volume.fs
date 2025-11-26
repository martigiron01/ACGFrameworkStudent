#version 330 core

in vec3 v_world_position;
out vec4 FragColor;

uniform vec3  u_camera_position;
uniform mat4  u_model;
uniform vec3  u_box_min;
uniform vec3  u_box_max;

uniform sampler3D u_texture;

uniform float u_step_length;
uniform vec4  u_background_color;

// Cut plane (xyz = normal, w = offset)
uniform vec4 u_cutoff;
// Transfer function for normalized CT [0..1].
vec3 transferFunction(float d)
{
    if (d < 0.25)
        return vec3(0.0);

    if (d < 0.6)
        return mix(vec3(0.0), vec3(1.0, 0.25, 0.25), (d - 0.25) / 0.35);

    return vec3(1.0);
}

vec2 intersectAABB(vec3 ro, vec3 rd, vec3 mn, vec3 mx)
{
    vec3 t1 = (mn - ro) / rd;
    vec3 t2 = (mx - ro) / rd;
    vec3 tmin = min(t1, t2);
    vec3 tmax = max(t1, t2);

    float tNear = max(max(tmin.x, tmin.y), tmin.z);
    float tFar  = min(min(tmax.x, tmax.y), tmax.z);

    return vec2(tNear, tFar);
}

void main()
{
    vec3 roW = u_camera_position;
    vec3 rdW = normalize(v_world_position - u_camera_position);

    mat4 invModel = inverse(u_model);
    vec3 ro = (invModel * vec4(roW, 1.0)).xyz;
    vec3 rd = normalize((invModel * vec4(rdW, 0.0)).xyz);

    vec2 hit = intersectAABB(ro, rd, u_box_min, u_box_max);
    float t0 = hit.x;
    float t1 = hit.y;

    if (t1 < 0.0 || t0 > t1)
        discard;

    t0 = max(t0, 0.0);

    float dt = u_step_length;

    vec3 color = vec3(0.0);
    float alpha = 0.0;

    for (float t = t0; t < t1; t += dt)
    {
        vec3 p = ro + rd * t;

        // Apply cut plane BEFORE sampling
        if (dot(u_cutoff.xyz, p) < u_cutoff.w)
            continue;

        vec3 uvw = (p - u_box_min) / (u_box_max - u_box_min);

        if (any(lessThan(uvw, vec3(0.0))) ||
            any(greaterThan(uvw, vec3(1.0))))
            continue;

        float d = texture(u_texture, uvw).r;

        vec3 c = transferFunction(d);
        float a = d;

        color += (1.0 - alpha) * a * c;
        alpha += (1.0 - alpha) * a;

        if (alpha > 0.99)
            break;
    }

    vec3 bg = u_background_color.rgb;
    vec3 final = mix(bg, color, alpha);

    FragColor = vec4(final, 1.0);
}
