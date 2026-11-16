using System;
using System.Linq;
using Godot;
using NightCero.Core.Math;
using NightCero.Core.WorldGen;
using NightCero.Core.WorldModel;

namespace NightCero.Game.Rendering;

/// <summary>
/// Draws a <see cref="TownPlan"/> as instanced boxes: ground, road grid, buildings and props.
/// </summary>
/// <remarks>
/// This is deliberately grey-box art: the renderer never invents geometry, it only gives the data
/// from the engine-free core a visual shape. One <see cref="MultiMeshInstance3D"/> per material
/// keeps a 724-building town in a handful of draw calls; the same layout can later be swapped for
/// prefab scenes without touching the generator. See <c>docs/art-direction.md</c>.
/// </remarks>
public partial class WorldRenderer : Node3D
{
    /// <summary>Height of one floor in metres, used to give each building its volume.</summary>
    public const float FloorHeight = 3.2f;

    private static readonly Color GroundColor = new(0.08f, 0.085f, 0.09f);
    private static readonly Color RoadColor = new(0.15f, 0.15f, 0.165f);
    private static readonly Color SidewalkColor = new(0.27f, 0.27f, 0.275f);
    private static readonly Color GrassColor = new(0.10f, 0.18f, 0.10f);
    private static readonly Color DirtColor = new(0.16f, 0.13f, 0.10f);
    private static readonly Color ParkPathColor = new(0.30f, 0.28f, 0.25f);
    private static readonly Color DoorColor = new(0.035f, 0.035f, 0.04f);
    private static readonly Color TrunkColor = new(0.14f, 0.11f, 0.08f);
    private static readonly Color CanopyColor = new(0.12f, 0.20f, 0.13f);
    private static readonly Color BenchColor = new(0.26f, 0.20f, 0.14f);
    private static readonly Color DumpsterColor = new(0.14f, 0.21f, 0.17f);
    private static readonly Color PoleColor = new(0.20f, 0.20f, 0.21f);
    private static readonly Color BulbColor = new(1.0f, 0.78f, 0.48f);
    private static readonly Color[] CarColors =
    [
        new Color(0.16f, 0.17f, 0.19f),
        new Color(0.20f, 0.12f, 0.10f),
        new Color(0.12f, 0.14f, 0.18f),
        new Color(0.19f, 0.19f, 0.17f),
    ];

    /// <summary>Builds the visual representation of <paramref name="plan"/>, replacing any previous one.</summary>
    public void Build(TownPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        AddChild(Ground(plan));
        AddChild(Roads(plan));
        AddChild(Blocks(plan));
        AddChild(ParkPaths(plan));
        AddChild(Buildings(plan));
        AddChild(Doors(plan));
        AddProps(plan);
    }

    // ---- Ground and roads ----------------------------------------------------------------

    private static MeshInstance3D Ground(TownPlan plan) => new()
    {
        Name = "Ground",
        Mesh = new PlaneMesh { Size = new Vector2(plan.Width + 256, plan.Height + 256) },
        Position = new Vector3(plan.Width / 2f, 0, plan.Height / 2f),
        MaterialOverride = Solid(GroundColor, roughness: 0.95f),
    };

    private static MultiMeshInstance3D Roads(TownPlan plan)
    {
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, plan.Roads.Count, RoadColor);
        for (var i = 0; i < plan.Roads.Count; i++)
        {
            var road = plan.Roads[i];
            var centre = (road.Start + road.End) * 0.5f;
            var scale = road.IsHorizontal
                ? new Vector3(road.Length, 0.06f, road.Width)
                : new Vector3(road.Width, 0.06f, road.Length);
            multimesh.SetInstanceTransform(i, Place(centre, scale));
        }

        return Wrap("Roads", multimesh);
    }

    // ---- Blocks: sidewalks, grass and empty lots -----------------------------------------

    /// <summary>Width of the sidewalk ring around a built block, in metres.</summary>
    private const float SidewalkWidth = 2f;

    /// <summary>
    /// The ground of every block. A built block gets a grass interior with a sidewalk ring around
    /// it, so the 15 cm step at the ring's outer edge reads as a curb; a park is grass with paths;
    /// an empty lot is bare ground.
    /// </summary>
    private static MultiMeshInstance3D Blocks(TownPlan plan)
    {
        var slabs = new List<(Vec3 Position, Vector3 Size, Color Color)>();

        foreach (var block in plan.Blocks)
        {
            var bounds = block.Bounds;
            var centre = new Vec3(bounds.Centre.X, 0, bounds.Centre.Y);

            switch (block.Kind)
            {
                case BlockKind.Park:
                    slabs.Add((centre, new Vector3(bounds.Width, 0.12f, bounds.Height), GrassColor));
                    break;

                case BlockKind.Empty:
                    slabs.Add((centre, new Vector3(bounds.Width, 0.10f, bounds.Height), DirtColor));
                    break;

                default:
                    var inner = bounds.Expand(-SidewalkWidth);
                    slabs.Add((
                        new Vec3(inner.Centre.X, 0, inner.Centre.Y),
                        new Vector3(inner.Width, 0.10f, inner.Height),
                        GrassColor));

                    var half = SidewalkWidth / 2f;
                    var ring = new Vector3(bounds.Width, 0.15f, SidewalkWidth);
                    var side = new Vector3(SidewalkWidth, 0.15f, bounds.Height - (SidewalkWidth * 2f));
                    slabs.Add((new Vec3(bounds.Centre.X, 0, bounds.MinY + half), ring, SidewalkColor));
                    slabs.Add((new Vec3(bounds.Centre.X, 0, bounds.MaxY - half), ring, SidewalkColor));
                    slabs.Add((new Vec3(bounds.MinX + half, 0, bounds.Centre.Y), side, SidewalkColor));
                    slabs.Add((new Vec3(bounds.MaxX - half, 0, bounds.Centre.Y), side, SidewalkColor));
                    break;
            }
        }

        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, slabs.Count, Colors.White);
        for (var i = 0; i < slabs.Count; i++)
        {
            var (position, size, color) = slabs[i];
            multimesh.SetInstanceTransform(
                i,
                Place(position + new Vec3(0, size.Y / 2f, 0), size));
            multimesh.SetInstanceColor(i, color);
        }

        return Wrap("Blocks", multimesh);
    }

    /// <summary>Two crossing paths through the middle of every park.</summary>
    private static MultiMeshInstance3D ParkPaths(TownPlan plan)
    {
        var parks = plan.Blocks.Where(block => block.Kind == BlockKind.Park).ToArray();
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, parks.Length * 2, ParkPathColor);

        for (var i = 0; i < parks.Length; i++)
        {
            var bounds = parks[i].Bounds;
            var centre = new Vec3(bounds.Centre.X, 0.15f, bounds.Centre.Y);
            multimesh.SetInstanceTransform(i * 2, Place(centre, new Vector3(bounds.Width, 0.06f, 1.6f)));
            multimesh.SetInstanceTransform((i * 2) + 1, Place(centre, new Vector3(1.6f, 0.06f, bounds.Height)));
        }

        return Wrap("ParkPaths", multimesh);
    }

    // ---- Buildings -----------------------------------------------------------------------

    private static MultiMeshInstance3D Buildings(TownPlan plan)
    {
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, plan.Buildings.Count, Colors.White);
        for (var i = 0; i < plan.Buildings.Count; i++)
        {
            var building = plan.Buildings[i];
            var height = building.Floors * FloorHeight;
            multimesh.SetInstanceTransform(
                i,
                Place(
                    building.Centre + new Vec3(0, height / 2f, 0),
                    new Vector3(building.Width, height, building.Depth)));
            multimesh.SetInstanceColor(i, ColorFor(building.Kind));
        }

        return Wrap("Buildings", multimesh);
    }

    /// <summary>
    /// A dark slab on the wall at each entrance. The generator already places the door on the side
    /// that faces the street, so the renderer only has to work out which wall it is on.
    /// </summary>
    private static MultiMeshInstance3D Doors(TownPlan plan)
    {
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, plan.Buildings.Count, DoorColor);

        for (var i = 0; i < plan.Buildings.Count; i++)
        {
            var building = plan.Buildings[i];
            var footprint = building.Footprint;
            var entrance = building.Entrance;
            const float halfWidth = 0.7f;
            const float height = 2.2f;
            const float thickness = 0.25f;
            const float lift = 0.15f;

            Vector3 size;
            Vec3 position;

            if (MathF.Abs(entrance.Z - footprint.MinY) < 0.01f)
            {
                size = new Vector3(halfWidth * 2f, height, thickness);
                position = new Vec3(entrance.X, lift + (height / 2f), footprint.MinY - (thickness / 2f));
            }
            else if (MathF.Abs(entrance.Z - footprint.MaxY) < 0.01f)
            {
                size = new Vector3(halfWidth * 2f, height, thickness);
                position = new Vec3(entrance.X, lift + (height / 2f), footprint.MaxY + (thickness / 2f));
            }
            else if (MathF.Abs(entrance.X - footprint.MinX) < 0.01f)
            {
                size = new Vector3(thickness, height, halfWidth * 2f);
                position = new Vec3(footprint.MinX - (thickness / 2f), lift + (height / 2f), entrance.Z);
            }
            else
            {
                size = new Vector3(thickness, height, halfWidth * 2f);
                position = new Vec3(footprint.MaxX + (thickness / 2f), lift + (height / 2f), entrance.Z);
            }

            multimesh.SetInstanceTransform(i, Place(position, size));
        }

        return Wrap("Doors", multimesh);
    }

    private static Color ColorFor(BuildingKind kind) => kind switch
    {
        BuildingKind.House => new Color(0.34f, 0.30f, 0.26f),
        BuildingKind.Shop => new Color(0.30f, 0.31f, 0.34f),
        BuildingKind.Warehouse => new Color(0.26f, 0.27f, 0.27f),
        BuildingKind.Garage => new Color(0.23f, 0.24f, 0.25f),
        BuildingKind.Church => new Color(0.38f, 0.36f, 0.32f),
        BuildingKind.Hospital => new Color(0.44f, 0.45f, 0.47f),
        _ => new Color(0.3f, 0.3f, 0.3f),
    };

    // ---- Props ---------------------------------------------------------------------------

    private void AddProps(TownPlan plan)
    {
        var trees = new List<Vec3>();
        var cars = new List<Prop>();
        var streetlights = new List<Vec3>();
        var benches = new List<Prop>();
        var dumpsters = new List<Vec3>();

        foreach (var prop in plan.Props)
        {
            switch (prop.Kind)
            {
                case PropKind.Tree:
                    trees.Add(prop.Position);
                    break;
                case PropKind.Car:
                    cars.Add(prop);
                    break;
                case PropKind.Streetlight:
                    streetlights.Add(prop.Position);
                    break;
                case PropKind.Bench:
                    benches.Add(prop);
                    break;
                case PropKind.Dumpster:
                    dumpsters.Add(prop.Position);
                    break;
            }
        }

        AddChild(Trees(trees));
        AddChild(Cars(cars));
        AddChild(Streetlights(streetlights));
        AddChild(Boxes("Benches", benches, new Vector3(1.8f, 0.45f, 0.6f), 0.225f, BenchColor));
        AddChild(SimpleBoxes("Dumpsters", dumpsters, new Vector3(1.5f, 1.1f, 1.0f), 0.55f, DumpsterColor));
    }

    private static Node3D Trees(List<Vec3> positions)
    {
        var group = new Node3D { Name = "Trees" };
        var trunks = Instanced(new CylinderMesh { Height = 1.2f, TopRadius = 0.18f, BottomRadius = 0.26f }, positions.Count, TrunkColor);
        var canopies = Instanced(new SphereMesh { Radius = 1f, Height = 2f }, positions.Count, CanopyColor);

        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            trunks.SetInstanceTransform(i, Place(p + new Vec3(0, 0.6f, 0), Vector3.One));
            canopies.SetInstanceTransform(i, Place(p + new Vec3(0, 2.6f, 0), new Vector3(2.2f, 2.6f, 2.2f)));
        }

        group.AddChild(Wrap("Trunks", trunks));
        group.AddChild(Wrap("Canopies", canopies));
        return group;
    }

    private static MultiMeshInstance3D Cars(List<Prop> cars)
    {
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, cars.Count, Colors.White);
        for (var i = 0; i < cars.Count; i++)
        {
            var car = cars[i];
            multimesh.SetInstanceTransform(
                i,
                Place(car.Position + new Vec3(0, 0.65f, 0), new Vector3(1.8f, 1.3f, 4.0f), car.YawDegrees));
            multimesh.SetInstanceColor(i, CarColors[i % CarColors.Length]);
        }

        return Wrap("Cars", multimesh);
    }

    private static Node3D Streetlights(List<Vec3> positions)
    {
        var group = new Node3D { Name = "Streetlights" };
        var poles = Instanced(new BoxMesh { Size = Vector3.One }, positions.Count, PoleColor);
        var bulbs = Instanced(new SphereMesh { Radius = 0.3f, Height = 0.6f }, positions.Count, BulbColor, emissive: true);

        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            poles.SetInstanceTransform(i, Place(p + new Vec3(0, 2.5f, 0), new Vector3(0.12f, 5f, 0.12f)));
            bulbs.SetInstanceTransform(i, Place(p + new Vec3(0, 5.1f, 0), Vector3.One));
        }

        group.AddChild(Wrap("Poles", poles));
        group.AddChild(Wrap("Bulbs", bulbs));
        return group;
    }

    private static MultiMeshInstance3D Boxes(string name, List<Prop> props, Vector3 size, float halfHeight, Color color)
    {
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, props.Count, color);
        for (var i = 0; i < props.Count; i++)
        {
            var prop = props[i];
            multimesh.SetInstanceTransform(
                i,
                Place(prop.Position + new Vec3(0, halfHeight, 0), size, prop.YawDegrees));
        }

        return Wrap(name, multimesh);
    }

    private static MultiMeshInstance3D SimpleBoxes(string name, List<Vec3> positions, Vector3 size, float halfHeight, Color color)
    {
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, positions.Count, color);
        for (var i = 0; i < positions.Count; i++)
        {
            multimesh.SetInstanceTransform(i, Place(positions[i] + new Vec3(0, halfHeight, 0), size));
        }

        return Wrap(name, multimesh);
    }

    // ---- Helpers -------------------------------------------------------------------------

    private static MultiMesh Instanced(Mesh mesh, int count, Color color, bool emissive = false)
    {
        var material = Solid(color, roughness: 0.9f);
        material.VertexColorUseAsAlbedo = true;
        if (emissive)
        {
            material.EmissionEnabled = true;
            material.Emission = color;
            material.EmissionEnergyMultiplier = 2.5f;
        }

        if (mesh is PrimitiveMesh primitive)
        {
            primitive.Material = material;
        }

        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = count,
        };

        for (var i = 0; i < count; i++)
        {
            multimesh.SetInstanceColor(i, color);
        }

        return multimesh;
    }

    private static StandardMaterial3D Solid(Color color, float roughness) => new()
    {
        AlbedoColor = color,
        Roughness = roughness,
        Metallic = 0f,
    };

    private static MultiMeshInstance3D Wrap(string name, MultiMesh multimesh) =>
        new() { Name = name, Multimesh = multimesh };

    private static Transform3D Place(Vec3 position, Vector3 scale, float yawDegrees = 0f)
    {
        var basis = new Basis(Vector3.Up, Mathf.DegToRad(yawDegrees)) * Basis.Identity.Scaled(scale);
        return new Transform3D(basis, new Vector3(position.X, position.Y, position.Z));
    }
}
