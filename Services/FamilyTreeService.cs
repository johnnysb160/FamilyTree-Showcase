using FamilyTree.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

// JSON consumed by the visualizations (JSInterop and /api)
public record FamilyNode(int Id, string Name, string Sex, string Years, string? Place, string? Photo,
    int? FatherId, int? MotherId, int? SpouseId, bool? IsDeceased, string? Relationship, bool IsRoot);

public record TreeNode(int Id, string Name, string Sex, string Years, string? Place, string? Photo, bool? IsDeceased,
    string? Role, string? Spouses, List<TreeNode> Children);

public record BidirectionalTree(TreeNode Ancestors, TreeNode Descendants, List<TreeNode> Siblings);

// Kinship = relationship to the person in focus (the point of view chosen on screen)
// Spouses = registered in the admin; CoParents = only have children in common (no marriage assumed)
public record RelativeDetails(Relative Person, Relative? Father, Relative? Mother, List<Relative> Spouses, List<Relative> CoParents,
    List<Relative> Children, List<Relative> Siblings, string? Kinship);

public class FamilyTreeService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Relative>> ListAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Relatives.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<int> GetRootIdAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rootId = await db.Relatives.Where(r => r.IsRoot).Select(r => r.Id).FirstOrDefaultAsync();
        return rootId > 0 ? rootId : await db.Relatives.Select(r => r.Id).FirstOrDefaultAsync();
    }

    public static int RootId(List<Relative> all) =>
        all.FirstOrDefault(r => r.IsRoot)?.Id ?? all.FirstOrDefault()?.Id ?? 0;

    public static string? Place(Relative r) =>
        r.BirthPlace is null ? r.BirthCountry : $"{r.BirthPlace}{(r.BirthCountry is null ? "" : $", {r.BirthCountry}")}";

    // Cards for the whole family, labeled from the point of view of the person in focus
    public static List<FamilyNode> Family(List<Relative> all, int? focusId = null)
    {
        var focus = focusId ?? RootId(all);
        return all.Select(r => new FamilyNode(r.Id, r.Name, r.Sex, r.Years, Place(r), r.PhotoUrl,
            r.FatherId, r.MotherId, r.SpouseId, r.IsDeceased,
            Label(all, focus, r), r.IsRoot)).ToList();
    }

    // Short label relative to the person in focus, computed from the tree.
    // The free-text "Relationship" typed in the admin is written from the root's point of view,
    // so it is only used (trimmed) when the focus is the root; free-text notes stay in the details drawer.
    public static string? Label(List<Relative> all, int focusId, Relative r)
    {
        if (Kinship(all, focusId, r.Id) is { } k) return k;
        if (focusId != RootId(all)) return "Família estendida";
        if (string.IsNullOrWhiteSpace(r.Relationship)) return null;
        var text = System.Text.RegularExpressions.Regex.Replace(r.Relationship, @"\s*[\(\-–].*$", "").Trim();
        return text.Length == 0 ? null : text;
    }

    // Root -> father/mother -> grandparents ... (children = parents)
    public static TreeNode Ancestors(List<Relative> all, int id)
    {
        var map = all.ToDictionary(r => r.Id);
        TreeNode Build(Relative r, string? role, HashSet<int> path)
        {
            var parents = new List<TreeNode>();
            if (path.Add(r.Id))
            {
                if (r.FatherId is { } f && map.TryGetValue(f, out var father)) parents.Add(Build(father, "Pai", path));
                if (r.MotherId is { } m && map.TryGetValue(m, out var mother)) parents.Add(Build(mother, "Mãe", path));
                path.Remove(r.Id);
            }
            return Node(r, role, null, parents);
        }
        return Build(map[id], null, []);
    }

    // Root -> children -> grandchildren ... (children = children)
    public static TreeNode Descendants(List<Relative> all, int id)
    {
        var map = all.ToDictionary(r => r.Id);
        TreeNode Build(Relative r, string? role, HashSet<int> path)
        {
            var children = new List<TreeNode>();
            if (path.Add(r.Id))
            {
                foreach (var c in ChildrenOf(all, r.Id).OrderBy(c => c.BirthDate ?? DateOnly.MaxValue))
                    children.Add(Build(c, c.Sex == "F" ? "Filha" : "Filho", path));
                path.Remove(r.Id);
            }
            return Node(r, role, SpouseNames(all, r), children);
        }
        return Build(map[id], null, []);
    }

    // Siblings with their own descendants (nephews, grand-nephews...), labeled relative to the person
    static List<TreeNode> SiblingTrees(List<Relative> all, Relative person) =>
        SiblingsOf(all, person).Select(s => Descendants(all, s.Id) with { Role = SiblingRole(person, s) }).ToList();

    // Treegraph ("Linhagem") data. Every person on the line gets a "Irmãos" branch with their siblings
    // and the siblings' descendants (uncles, great-uncles, cousins...), so nobody in the blood line is left out.
    // Group nodes have Id 0 so the front end can style them and skip the details drawer.
    public static TreeNode Treegraph(List<Relative> all, int id, bool ancestors)
    {
        var map = all.ToDictionary(r => r.Id);
        if (!ancestors) return WithSiblings(Descendants(all, id), map[id]);

        TreeNode Build(Relative r, string? role, HashSet<int> path)
        {
            var branches = new List<TreeNode>();
            if (path.Add(r.Id))
            {
                if (r.FatherId is { } f && map.TryGetValue(f, out var father)) branches.Add(Build(father, "Pai", path));
                if (r.MotherId is { } m && map.TryGetValue(m, out var mother)) branches.Add(Build(mother, "Mãe", path));
                path.Remove(r.Id);
            }
            return WithSiblings(Node(r, role, SpouseNames(all, r), branches), r);
        }
        return Build(map[id], null, []);

        TreeNode WithSiblings(TreeNode node, Relative r)
        {
            var siblings = SiblingTrees(all, r);
            if (siblings.Count == 0) return node;
            var group = new TreeNode(0, "Irmãos", "", siblings.Count == 1 ? "1 pessoa" : $"{siblings.Count} pessoas", null, null, null, null, null, siblings);
            return node with { Children = [.. node.Children, group] };
        }
    }

    static string? SpouseNames(List<Relative> all, Relative r)
    {
        var names = string.Join(", ", RegisteredSpousesOf(all, r).Select(s => s.Name).Append(r.SpouseName).Where(n => !string.IsNullOrEmpty(n)));
        return names.Length == 0 ? null : names;
    }

    public static BidirectionalTree Bidirectional(List<Relative> all, int id)
    {
        return new(Ancestors(all, id), Descendants(all, id), SiblingTrees(all, all.First(r => r.Id == id)));
    }

    public static RelativeDetails? GetDetails(List<Relative> all, int id, int focusId)
    {
        var map = all.ToDictionary(r => r.Id);
        if (!map.TryGetValue(id, out var r)) return null;
        return new RelativeDetails(r,
            r.FatherId is { } f ? map.GetValueOrDefault(f) : null,
            r.MotherId is { } m ? map.GetValueOrDefault(m) : null,
            RegisteredSpousesOf(all, r).ToList(),
            CoParentsOf(all, r).ToList(),
            ChildrenOf(all, id).OrderBy(c => c.BirthDate ?? DateOnly.MaxValue).ToList(),
            SiblingsOf(all, r).ToList(),
            Label(all, focusId, r));
    }

    // On-screen label (Portuguese)
    public static string SiblingRole(Relative person, Relative sibling)
    {
        var label = sibling.Sex == "F" ? "Irmã" : "Irmão";
        if (sibling.FatherId == person.FatherId && sibling.MotherId == person.MotherId) return label;
        return sibling.FatherId == person.FatherId && person.FatherId is not null ? $"{label} por parte de pai" : $"{label} por parte de mãe";
    }

    static TreeNode Node(Relative r, string? role, string? spouses, List<TreeNode> children) =>
        new(r.Id, r.Name, r.Sex, r.Years, Place(r), r.PhotoUrl, r.IsDeceased, role, spouses, children);

    public static IEnumerable<Relative> ChildrenOf(List<Relative> all, int id) =>
        all.Where(c => c.FatherId == id || c.MotherId == id);

    // Spouse registered in the admin (either side of the link)
    public static IEnumerable<Relative> RegisteredSpousesOf(List<Relative> all, Relative r) =>
        all.Where(x => x.Id != r.Id && (x.Id == r.SpouseId || x.SpouseId == r.Id));

    // People who only have a child in common with r (not registered as spouse)
    public static IEnumerable<Relative> CoParentsOf(List<Relative> all, Relative r)
    {
        var spouses = RegisteredSpousesOf(all, r).Select(x => x.Id).ToHashSet();
        var ids = ChildrenOf(all, r.Id).Select(c => c.FatherId == r.Id ? c.MotherId : c.FatherId)
            .OfType<int>().Where(id => id != r.Id && !spouses.Contains(id)).ToHashSet();
        return all.Where(x => ids.Contains(x.Id));
    }

    // Partners for suggestions in the admin = registered spouses + co-parents
    public static IEnumerable<Relative> SpousesOf(List<Relative> all, Relative r)
    {
        var ids = ChildrenOf(all, r.Id).Select(c => c.FatherId == r.Id ? c.MotherId : c.FatherId)
            .Append(r.SpouseId)
            .Concat(all.Where(x => x.SpouseId == r.Id).Select(x => (int?)x.Id))
            .OfType<int>().Where(id => id != r.Id).ToHashSet();
        return all.Where(x => ids.Contains(x.Id));
    }

    public static IEnumerable<Relative> SiblingsOf(List<Relative> all, Relative r) =>
        all.Where(s => s.Id != r.Id &&
                       ((r.FatherId is not null && s.FatherId == r.FatherId) || (r.MotherId is not null && s.MotherId == r.MotherId)))
           .OrderBy(s => s.BirthDate ?? DateOnly.MaxValue);

    // ---------- Kinship (relationship to the root, computed from the graph; labels in Portuguese) ----------

    public static string? Kinship(List<Relative> all, int rootId, int personId)
    {
        if (rootId == personId) return "Você";
        var map = all.ToDictionary(r => r.Id);
        if (!map.TryGetValue(personId, out var person) || !map.ContainsKey(rootId)) return null;

        if (BloodKinship(map, rootId, person) is { } blood) return blood;

        // Marriage words only for spouses registered in the admin
        foreach (var spouse in RegisteredSpousesOf(all, person))
        {
            if (spouse.Id == rootId) return person.Sex == "F" ? "Esposa" : "Marido";
            if (BloodKinship(map, rootId, spouse) is { } k) return ByMarriage(k, person, spouse);
        }

        // Otherwise describe only what the data says: parent of a relative ("Mãe do seu irmão Cléber")...
        var kids = ChildrenOf(all, person.Id)
            .Select(c => (Person: c, Kin: c.Id == rootId ? null : BloodKinship(map, rootId, c)))
            .Where(c => c.Kin is not null).ToList();
        if (kids.Count > 0) return $"{(person.Sex == "F" ? "Mãe" : "Pai")} {Of(kids)}";

        // ...or sibling of a relative ("Irmã do seu tio Cledir")
        var siblings = SiblingsOf(all, person)
            .Select(c => (Person: c, Kin: c.Id == rootId ? null : BloodKinship(map, rootId, c)))
            .Where(c => c.Kin is not null).ToList();
        if (siblings.Count > 0) return $"{(person.Sex == "F" ? "Irmã" : "Irmão")} {Of(siblings)}";
        return null;
    }

    // "do seu irmão Rafael", "dos seus filhos Clara e Theo", "da sua prima Gabriela"
    static string Of(List<(Relative Person, string? Kin)> people)
    {
        // Base word of the kinship ("Irmão por parte de pai" -> "irmão", "Tio paterno" -> "tio")
        static string Word(string kin) => kin.Split(' ')[0].ToLowerInvariant();
        static string Plural(string w) => w.EndsWith("ão") ? w[..^2] + "ãos" : w + "s";
        // filho/filha, irmão/irmã... count as the same word (Portuguese plural uses the masculine)
        var words = people.Select(p => Word(p.Kin!)).ToList();
        var shared = words.Select(MasculineOf).Distinct().ToList();
        var names = people.Select(p => p.Person.Name.Split(' ')[0]).ToList();
        var list = names.Count == 1 ? names[0] : $"{string.Join(", ", names[..^1])} e {names[^1]}";
        var allFemale = people.All(p => p.Person.Sex == "F");
        if (people.Count == 1) return $"{(allFemale ? "da sua" : "do seu")} {words[0]} {list}";
        // Different kinships (e.g. a nephew and a cousin): "parentes"
        if (shared.Count > 1) return $"dos seus parentes {list}";
        return allFemale ? $"das suas {Plural(words[0])} {list}" : $"dos seus {Plural(shared[0])} {list}";
    }

    static string MasculineOf(string w) => w switch
    {
        "irmã" => "irmão", "filha" => "filho", "prima" => "primo", "sobrinha" => "sobrinho", "tia" => "tio",
        "neta" => "neto", "avó" => "avô", "sobrinha-neta" => "sobrinho-neto", _ => w
    };

    static Dictionary<int, int> AncestorDepths(Dictionary<int, Relative> map, int id)
    {
        var depth = new Dictionary<int, int> { [id] = 0 };
        var queue = new Queue<int>([id]);
        while (queue.Count > 0)
        {
            var cur = map[queue.Dequeue()];
            foreach (var p in new[] { cur.FatherId, cur.MotherId }.OfType<int>())
                if (map.ContainsKey(p) && depth.TryAdd(p, depth[cur.Id] + 1)) queue.Enqueue(p);
        }
        return depth;
    }

    static string? BloodKinship(Dictionary<int, Relative> map, int rootId, Relative person)
    {
        var fromRoot = AncestorDepths(map, rootId);
        var fromPerson = AncestorDepths(map, person.Id);
        var common = fromRoot.Keys.Where(fromPerson.ContainsKey)
            .Select(a => (Ancestor: a, Up: fromRoot[a], Down: fromPerson[a]))
            .OrderBy(c => c.Up + c.Down).ThenBy(c => c.Up).ToList();
        if (common.Count == 0) return null;
        var (ancestor, up, down) = common[0];
        var f = person.Sex == "F";
        string G(string m, string w) => f ? w : m;

        // Paternal/maternal side for relatives beyond the parents
        var root = map[rootId];
        string Side()
        {
            if (up < 2) return "";
            var paternal = root.FatherId is { } fa && map.ContainsKey(fa) && AncestorDepths(map, fa).TryGetValue(ancestor, out var d) && d == up - 1;
            return paternal ? G(" paterno", " paterna") : G(" materno", " materna");
        }

        if (down == 0)
            return up switch
            {
                1 => G("Pai", "Mãe"),
                2 => G("Avô", "Avó") + Side(),
                3 => G("Bisavô", "Bisavó") + Side(),
                4 => G("Trisavô", "Trisavó") + Side(),
                5 => G("Tetravô", "Tetravó") + Side(),
                _ => $"Ancestral ({up} gerações)"
            };
        if (up == 0)
            return down switch
            {
                1 => G("Filho", "Filha"),
                2 => G("Neto", "Neta"),
                3 => G("Bisneto", "Bisneta"),
                4 => G("Trineto", "Trineta"),
                _ => $"Descendente ({down} gerações)"
            };
        if (up == 1 && down == 1)
        {
            var label = G("Irmão", "Irmã");
            if (person.FatherId == root.FatherId && person.MotherId == root.MotherId) return label;
            return ancestor == root.FatherId ? $"{label} por parte de pai" : $"{label} por parte de mãe";
        }
        if (down == 1)
            return up switch
            {
                2 => G("Tio", "Tia") + Side(),
                3 => G("Tio-avô", "Tia-avó") + Side(),
                4 => G("Tio-bisavô", "Tia-bisavó") + Side(),
                _ => G("Tio", "Tia") + $" ({up - 1} gerações acima)"
            };
        if (up == 1)
            return down switch
            {
                2 => G("Sobrinho", "Sobrinha"),
                3 => G("Sobrinho-neto", "Sobrinha-neta"),
                _ => G("Sobrinho", "Sobrinha") + $" ({down - 1} gerações abaixo)"
            };
        if (up == down) return up == 2 ? G("Primo", "Prima") + Side() : G("Primo", "Prima") + $" de {up - 1}º grau";
        if (up == down + 1)
        {
            var parentSide = Side().Contains("pater") ? "do pai" : "da mãe";
            return G("Primo", "Prima") + $" {parentSide}";
        }
        if (down == up + 1) return G("Filho", "Filha") + " de primo";
        return G("Primo", "Prima") + $" de {Math.Max(up, down) - 1}º grau";
    }

    static string ByMarriage(string spouseKinship, Relative person, Relative spouse)
    {
        var f = person.Sex == "F";
        var k = spouseKinship;
        if (k.StartsWith("Irmão") || k.StartsWith("Irmã")) return f ? "Cunhada" : "Cunhado";
        if (k is "Filho" or "Filha") return f ? "Nora" : "Genro";
        if (k is "Pai" or "Mãe") return f ? "Madrasta" : "Padrasto";
        if (k.StartsWith("Tio") || k.StartsWith("Tia"))
        {
            // Same title as the uncle/aunt, with this person's gender
            var parts = k.Split(' ', 2);
            var title = parts[0] switch
            {
                "Tio" or "Tia" => f ? "Tia" : "Tio",
                "Tio-avô" or "Tia-avó" => f ? "Tia-avó" : "Tio-avô",
                "Tio-bisavô" or "Tia-bisavó" => f ? "Tia-bisavó" : "Tio-bisavô",
                var other => other
            };
            var side = parts.Length < 2 ? "" : " " + (parts[1].StartsWith("pat") ? (f ? "paterna" : "paterno") : parts[1].StartsWith("mat") ? (f ? "materna" : "materno") : parts[1]);
            return $"{title}{side} (por afinidade)";
        }
        var link = spouse.Sex == "F" ? "da" : "do";
        return $"{(f ? "Esposa" : "Marido")} {link} {k.ToLowerInvariant()}";
    }
}
