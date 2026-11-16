shader_type sky;

// Procedural night sky: gradient, drifting cloud deck, stars and a moon.
// One full-screen pass with a three-octave value noise, so it runs on the Compatibility backend
// and on integrated GPUs. Parameters come from SkyLook.cs.

uniform vec3 top_color : source_color = vec3(0.01, 0.014, 0.03);
uniform vec3 horizon_color : source_color = vec3(0.045, 0.052, 0.085);
uniform vec3 moon_direction = vec3(-0.45, 0.55, -0.7);
uniform vec3 moon_color : source_color = vec3(0.88, 0.92, 1.0);
uniform float moon_size = 0.022;
uniform float moon_glow = 0.55;
uniform float star_density = 0.9965;
uniform float star_brightness = 1.0;
uniform float cloud_coverage = 0.42;
uniform vec3 cloud_color : source_color = vec3(0.075, 0.085, 0.115);
uniform float cloud_speed = 0.004;

float hash21(vec2 p)
{
	// Cheap 2D hash: enough entropy for stars and noise, no texture lookups.
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float value_noise(vec2 p)
{
	vec2 cell = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	float a = hash21(cell);
	float b = hash21(cell + vec2(1.0, 0.0));
	float c = hash21(cell + vec2(0.0, 1.0));
	float d = hash21(cell + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p)
{
	float sum = 0.0;
	float amplitude = 0.5;
	float norm = 0.0;
	mat2 turn = mat2(vec2(0.80, 0.60), vec2(-0.60, 0.80));
	for (int i = 0; i < 5; i++)
	{
		sum += value_noise(p) * amplitude;
		norm += amplitude;
		p = turn * (p * 2.02);
		amplitude *= 0.5;
	}
	return sum / norm;
}

// Domain-warped fbm: warping the coordinates with a second fbm breaks the grid of a plain value
// noise and turns the cloud into curled banks instead of stacked squares.
float cloud_density(vec2 p)
{
	vec2 warp = vec2(fbm(p + vec2(4.1, 1.7)), fbm(p + vec2(9.3, 7.4)));
	return fbm(p + ((warp - 0.5) * 1.3));
}

void sky()
{
	vec3 direction = EYEDIR;
	float height = clamp(direction.y, 0.0, 1.0);

	// Gradient: the horizon is the bright band the fog blends into.
	vec3 color = mix(horizon_color, top_color, pow(height, 0.45));

	// Stars, only above the horizon and only where a hashed cell passes the density threshold.
	// Each star twinkles on its own phase so the sky does not pulse as a whole.
	if (star_brightness > 0.0 && direction.y > 0.02)
	{
		vec2 plane = direction.xz / max(direction.y, 0.08);
		vec2 cell = floor(plane * 90.0);
		float roll = hash21(cell);
		if (roll > star_density)
		{
			vec2 local = fract(plane * 90.0) - 0.5;
			float core = smoothstep(0.42, 0.0, length(local));
			float twinkle = 0.65 + (0.35 * sin(TIME * 1.7 + (roll * 91.0)));
			float fade = smoothstep(0.02, 0.25, direction.y);
			color += vec3(0.85, 0.90, 1.0) * core * twinkle * fade * star_brightness;
		}
	}

	// Cloud deck: fbm on a plane above the viewer, drifting with time. The plane is clamped hard
	// near the horizon, where the projection would otherwise stretch one noise cell into a band.
	// The transition band is wide (0.55) so the deck has soft edges instead of facets.
	if (cloud_coverage > 0.001 && direction.y > -0.02)
	{
		vec2 sky_plane = direction.xz / max(direction.y + 0.35, 0.35);
		vec2 drift = vec2(TIME * cloud_speed, TIME * cloud_speed * 0.4);
		float density = cloud_density((sky_plane * 2.2) + drift);
		float threshold = 1.0 - cloud_coverage;
		float cover = smoothstep(threshold - 0.22, threshold + 0.30, density);
		cover *= smoothstep(-0.02, 0.28, direction.y);

		// Silver lining: cloud picks up a little moon colour, so the deck is lit rather than flat.
		float to_moon = max(0.0, dot(direction, normalize(moon_direction)));
		vec3 lit_cloud = cloud_color + (moon_color * pow(to_moon, 6.0) * 0.35);
		color = mix(color, lit_cloud, cover * 0.90);
	}

	// The moon: a hard disc with a soft halo. Its light comes from the same direction as the key
	// light, so shadows on the ground and the moon in the sky agree.
	float moon_angle = acos(clamp(dot(direction, normalize(moon_direction)), -1.0, 1.0));
	float disc = smoothstep(moon_size, moon_size * 0.72, moon_angle);
	float halo = pow(max(0.0, 1.0 - (moon_angle / max(moon_glow, 0.001))), 3.0) * 0.5;
	color += moon_color * (disc * 1.6 + halo);

	COLOR = color;
}
