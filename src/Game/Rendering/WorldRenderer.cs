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
/// <para>
/// This is deliberately grey-box art: the renderer never invents geometry, it only gives the data
/// from the engine-free core a visual shape. One <see cref="MultiMeshInstance3D"/> per material
/// keeps a 724-building town in a handful of draw calls; the same layout can later be swapped for
/// prefab scenes without touching the generator.
/// </para>
/// <para>
/// No colour lives in this file: everything comes from <see cref="Palette"/>, so the whole look can
/// be retuned in one place. See <c>docs/art-direction.md</c>.
/// </para>
/// </remarks>
public partial class WorldRenderer : Node3D
{
    /// <summary>Height of one floor in metres, used to give each building its volume.</summary>
    public const float FloorHeight = 3.2f;

    /// <summary>Width of the sidewalk ring around a built block, in metres.</summary>
    private const float SidewalkWidth = 2f;

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
        AddChild(RoadMarkings(plan));
        AddChild(Blocks(plan));
        AddChild(ParkPaths(plan));
        AddChild(Buildings(plan));
        AddChild(Doors(plan));
        AddChild(Windows(plan));
        AddChild(Hatches(plan));
        AddProps(plan);
    }

    // ---- Ground and roads ----------------------------------------------------------------

    private static MeshInstance3D Ground(TownPlan plan) => new()
    {
        Name = "Ground",
        Mesh = new PlaneMesh { Size = new Vector2(plan.Width + 256, plan.Height + 256) },
        Position = new Vector3(plan.Width / 2f, 0, plan.Height / 2f),
        MaterialOverride = Solid(Palette.Ground, roughness: 0.95f),
    };

    private static MultiMeshInstance3D Roads(TownPlan plan)
    {
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, plan.Roads.Count, Palette.Road);
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

    /// <summary>
    /// A dashed centre line down every road. It is what makes the asphalt read as a street from
    /// the camera height rather than a grey strip.
    /// </summary>
    private static MultiMeshInstance3D RoadMarkings(TownPlan plan)
    {
        const float dash = 3f;
        const float gap = 6f;
        const float width = 0.18f;

        var dashes = plan.Roads.Sum(road => (int)(road.Length / (dash + gap)));
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, dashes, Palette.RoadMarking);
        var index = 0;

        foreach (var road in plan.Roads)
        {
            for (var d = 0; d < (int)(road.Length / (dash + gap)); d++)
            {
                var along = (d * (dash + gap)) + (dash * 0.5f);
                var position = road.Start + ((road.End - road.Start).Normalized() * along);
                position = new Vec3(position.X, 0.075f, position.Z);
                var scale = road.IsHorizontal
                    ? new Vector3(dash, 0.02f, width)
                    : new Vector3(width, 0.02f, dash);
                multimesh.SetInstanceTransform(index++, Place(position, scale));
            }
        }

        return Wrap("RoadMarkings", multimesh);
    }

    // ---- Blocks: sidewalks, grass, woods and empty lots ----------------------------------

    /// <summary>
    /// The ground of every block. A built block gets a grass interior with a sidewalk ring around
    /// it, so the 15 cm step at the ring's outer edge reads as a curb; a park is grass with paths;
    /// a wood is leaf litter under dense trees; an empty lot is bare ground.
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
                    slabs.Add((centre, new Vector3(bounds.Width, 0.12f, bounds.Height), Palette.Grass));
                    break;

                case BlockKind.Forest:
                    slabs.Add((centre, new Vector3(bounds.Width, 0.12f, bounds.Height), Palette.ForestFloor));
                    break;

                case BlockKind.Empty:
                    slabs.Add((centre, new Vector3(bounds.Width, 0.10f, bounds.Height), Palette.Dirt));
                    break;

                default:
                    var inner = bounds.Expand(-SidewalkWidth);
                    slabs.Add((
                        new Vec3(inner.Centre.X, 0, inner.Centre.Y),
                        new Vector3(inner.Width, 0.10f, inner.Height),
                        Palette.Grass));

                    var half = SidewalkWidth / 2f;
                    var ring = new Vector3(bounds.Width, 0.15f, SidewalkWidth);
                    var side = new Vector3(SidewalkWidth, 0.15f, bounds.Height - (SidewalkWidth * 2f));
                    slabs.Add((new Vec3(bounds.Centre.X, 0, bounds.MinY + half), ring, Palette.Sidewalk));
                    slabs.Add((new Vec3(bounds.Centre.X, 0, bounds.MaxY - half), ring, Palette.Sidewalk));
                    slabs.Add((new Vec3(bounds.MinX + half, 0, bounds.Centre.Y), side, Palette.Sidewalk));
                    slabs.Add((new Vec3(bounds.MaxX - half, 0, bounds.Centre.Y), side, Palette.Sidewalk));
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
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, parks.Length * 2, Palette.ParkPath);

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
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, plan.Buildings.Count, Palette.Door);

        for (var i = 0; i < plan.Buildings.Count; i++)
        {
            var building = plan.Buildings[i];
            var wall = WallOf(building);
            var height = 2.2f;
            var width = 0.7f;
            var thickness = 0.25f;
            const float lift = 0.15f;
            var centre = building.Entrance + new Vec3(0, lift + (height / 2f), 0);

            var size = wall.IsAlongX
                ? new Vector3(width * 2f, height, thickness)
                : new Vector3(thickness, height, width * 2f);
            multimesh.SetInstanceTransform(i, Place(centre + (wall.Outward * (thickness / 2f)), size));
        }

        return Wrap("Doors", multimesh);
    }

    /// <summary>
    /// One dark pane per 3 m of street-facing wall and per floor. A few of them are lit, chosen by
    /// a deterministic rule from the building id so the same seed always lights the same windows.
    /// </summary>
    private static MultiMeshInstance3D Windows(TownPlan plan)
    {
        const float spacing = 3.2f;
        const float edge = 1.6f;
        const float width = 1.1f;
        const float height = 1.4f;
        const float thickness = 0.12f;

        var panes = new List<(Vec3 Centre, Vector3 Size, Color Color)>(plan.Buildings.Count * 6);

        foreach (var building in plan.Buildings)
        {
            var wall = WallOf(building);
            var footprint = building.Footprint;
            var length = wall.IsAlongX ? footprint.Width : footprint.Height;
            var centres = (int)((length - (edge * 2f)) / spacing) + 1;
            if (centres <= 0)
            {
                continue;
            }

            var first = -((centres - 1) * spacing) / 2f;

            for (var floor = 0; floor < building.Floors; floor++)
            {
                for (var k = 0; k < centres; k++)
                {
                    var offset = first + (k * spacing);
                    var basePoint = wall.IsAlongX
                        ? new Vec3(building.Entrance.X + offset, 0, wall.Plane)
                        : new Vec3(wall.Plane, 0, building.Entrance.Z + offset);
                    var centre = basePoint + new Vec3(
                        0,
                        (floor * FloorHeight) + 1.75f,
                        0) + (wall.Outward * (thickness / 2f));

                    var size = wall.IsAlongX
                        ? new Vector3(width, height, thickness)
                        : new Vector3(thickness, height, width);

                    // 1 in 9 windows is lit from inside: sparse enough to read as an accident of
                    // the night, dense enough that a street is not a black wall.
                    var lit = ((building.Id * 37) + (floor * 11) + k) % 9 == 0;
                    panes.Add((centre, size, lit ? Palette.LitWindow : Palette.Window));
                }
            }
        }

        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, panes.Count, Colors.White);
        for (var i = 0; i < panes.Count; i++)
        {
            var (centre, size, color) = panes[i];
            multimesh.SetInstanceTransform(i, Place(centre, size));
            multimesh.SetInstanceColor(i, color);
        }

        return Wrap("Windows", multimesh);
    }

    /// <summary>
    /// The cellar door of every basement and the hatch down to the catacombs. The catacomb one is
    /// marked with the accent colour, exactly like the other night lights, so it reads as an
    /// invitation rather than a label.
    /// </summary>
    private static Node3D Hatches(TownPlan plan)
    {
        var cellars = plan.Buildings.Where(building => building.HasBasement).ToArray();
        var catacombs = plan.Buildings.Where(building => building.CatacombEntrance).ToArray();

        var group = new Node3D { Name = "Hatches" };
        group.AddChild(HatchSlabs("CellarHatches", cellars, Palette.Window, emissive: false));
        group.AddChild(HatchSlabs("CatacombHatches", catacombs, Palette.Accent, emissive: true));
        return group;
    }

    private static MultiMeshInstance3D HatchSlabs(
        string name,
        IReadOnlyList<Building> buildings,
        Color color,
        bool emissive)
    {
        var multimesh = Instanced(new BoxMesh { Size = Vector3.One }, buildings.Count, color, emissive);

        for (var i = 0; i < buildings.Count; i++)
        {
            var building = buildings[i];
            var footprint = building.Footprint;

            // A cellar door sits in the yard, on the corner away from the street door.
            var position = new Vec3(footprint.MinX + 1.4f, 0.18f, footprint.MinY + 1.4f);
            multimesh.SetInstanceTransform(i, Place(position, new Vector3(1.2f, 0.1f, 1.2f)));
        }

        return Wrap(name, multimesh);
    }

    private static Color ColorFor(BuildingKind kind) => kind switch
    {
        BuildingKind.House => Palette.House,
        BuildingKind.Mansion => Palette.Mansion,
        BuildingKind.Shop => Palette.Shop,
        BuildingKind.Warehouse => Palette.Warehouse,
        BuildingKind.Garage => Palette.Garage,
        BuildingKind.Church => Palette.Church,
        BuildingKind.Hospital => Palette.Hospital,
        BuildingKind.Police => Palette.Police,
        _ => Palette.Unknown,
    };

    /// <summary>The outer wall a building's door is on, in world space.</summary>
    private readonly record struct Wall(bool IsAlongX, float Plane, Vec3 Outward);

    private static Wall WallOf(Building building)
    {
        var footprint = building.Footprint;
        var entrance = building.Entrance;

        if (MathF.Abs(entrance.Z - footprint.MinY) < 0.01f)
        {
            return new Wall(IsAlongX: true, Plane: footprint.MinY, Outward: new Vec3(0, 0, -1));
        }

        if (MathF.Abs(entrance.Z - footprint.MaxY) < 0.01f)
        {
            return new Wall(IsAlongX: true, Plane: footprint.MaxY, Outward: new Vec3(0, 0, 1));
        }

        return MathF.Abs(entrance.X - footprint.MinX) < 0.01f
            ? new Wall(IsAlongX: false, Plane: footprint.MinX, Outward: new Vec3(-1, 0, 0))
            : new Wall(IsAlongX: false, Plane: footprint.MaxX, Outward: new Vec3(1, 0, 0));
    }

    // ---- Props ---------------------------------------------------------------------------

    private void AddProps(TownPlan plan)
    {
        var parkTrees = new List<Vec3>();
        var forestTrees = new List<Vec3>();
        var cars = new List<Prop>();
        var streetlights = new List<Vec3>();
        var benches = new List<Prop>();
        var dumpsters = new List<Vec3>();

        foreach (var prop in plan.Props)
        {
            switch (prop.Kind)
            {
                case PropKind.Tree:
                    (InWood(plan, prop.Position) ? forestTrees : parkTrees).Add(prop.Position);
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

        AddChild(Conifers(forestTrees));
        AddChild(BroadleafTrees(parkTrees));
        AddChild(Cars(cars));
        AddChild(Streetlights(streetlights));
        AddChild(Boxes("Benches", benches, new Vector3(1.8f, 0.45f, 0.6f), 0.225f, Palette.Bench));
        AddChild(SimpleBoxes("Dumpsters", dumpsters, new Vector3(1.5f, 1.1f, 1.0f), 0.55f, Palette.Dumpster));
    }

    private static bool InWood(TownPlan plan, Vec3 position)
    {
        var point = new Vec2(position.X, position.Z);
        foreach (var block in plan.Blocks)
        {
            if (block.Kind == BlockKind.Forest && block.Bounds.Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A conifer: one trunk and three stacked cones. The silhouette — tall, narrow, ragged — is
    /// what reads as a forest from a distance, which a sphere never does.
    /// </summary>
    private static Node3D Conifers(List<Vec3> positions)
    {
        var group = new Node3D { Name = "Conifers" };
        var trunkHeight = 3.2f;
        var trunks = Instanced(
            new CylinderMesh { Height = 1f, TopRadius = 0.14f, BottomRadius = 0.24f },
            positions.Count,
            Palette.Trunk);
        var cones = Instanced(
            new CylinderMesh { Height = 1f, TopRadius = 0.02f, BottomRadius = 1f, RadialSegments = 7 },
            positions.Count * 3,
            Colors.White);

        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            trunks.SetInstanceTransform(i, Place(p + new Vec3(0, trunkHeight / 2f, 0), new Vector3(1, trunkHeight, 1)));

            for (var tier = 0; tier < 3; tier++)
            {
                var t = tier / 2f;
                var radius = 1.5f - (t * 0.5f);
                var height = 3.0f - (t * 0.6f);
                var y = trunkHeight + (tier * 1.3f) + (height / 2f) - 0.4f;
                var index = (i * 3) + tier;
                cones.SetInstanceTransform(index, Place(p + new Vec3(0, y, 0), new Vector3(radius, height, radius)));

                // Darker at the bottom of the crown, where the light does not reach.
                cones.SetInstanceColor(index, Palette.Conifer.Darkened(tier * 0.10f));
            }
        }

        group.AddChild(Wrap("ConiferTrunks", trunks));
        group.AddChild(Wrap("ConiferCrowns", cones));
        return group;
    }

    /// <summary>Park trees keep the round crown: the town's planted trees, not the wild ones.</summary>
    private static Node3D BroadleafTrees(List<Vec3> positions)
    {
        var group = new Node3D { Name = "BroadleafTrees" };
        var trunks = Instanced(
            new CylinderMesh { Height = 1.2f, TopRadius = 0.18f, BottomRadius = 0.26f },
            positions.Count,
            Palette.Trunk);
        var canopies = Instanced(new SphereMesh { Radius = 1f, Height = 2f }, positions.Count, Palette.Canopy);

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
            multimesh.SetInstanceColor(i, Palette.Cars[i % Palette.Cars.Length]);
        }

        return Wrap("Cars", multimesh);
    }

    private static Node3D Streetlights(List<Vec3> positions)
    {
        var group = new Node3D { Name = "Streetlights" };
        var poles = Instanced(new BoxMesh { Size = Vector3.One }, positions.Count, Palette.Pole);
        var bulbs = Instanced(new SphereMesh { Radius = 0.3f, Height = 0.6f }, positions.Count, Palette.Accent, emissive: true);

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

    /// <summary>
    /// Emission strength of the accent lights. Kept under 2: above that the accent colour clips to
    /// white and the scene loses the one colour it has.
    /// </summary>
    private const float EmissionEnergy = 1.4f;

    private static MultiMesh Instanced(Mesh mesh, int count, Color color, bool emissive = false)
    {
        var material = Solid(color, roughness: 0.9f);
        material.VertexColorUseAsAlbedo = true;
        if (emissive)
        {
            material.EmissionEnabled = true;
            material.Emission = color;
            material.EmissionEnergyMultiplier = EmissionEnergy;
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
