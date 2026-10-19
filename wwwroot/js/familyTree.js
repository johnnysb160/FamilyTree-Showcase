// Family tree visualizations (D3 v7, loaded globally in App.razor).
// 1) familyTree — "Árvore familiar": whole family by generation, collapse up/down/sideways like OrgChart (dabeng)
// 2) treegraph        — "Linhagem": bezier tree with animations in the style of Highcharts Treegraph

let current = null;

export function destroy() {
    current?.destroy?.();
    current = null;
}

export function command(cmd, arg) {
    current?.[cmd]?.(arg);
}

// Point of view chosen by this visitor (browser storage is a convenience: it may be blocked)
const FOCUS_KEY = "familyTree.focus", VISITED_KEY = "familyTree.visited";
export function loadFocus() {
    try { const v = parseInt(localStorage.getItem(FOCUS_KEY)); return Number.isFinite(v) ? v : null; } catch { return null; }
}
export function saveFocus(id) {
    try { localStorage.setItem(FOCUS_KEY, String(id)); localStorage.setItem(VISITED_KEY, "1"); } catch { }
}
export function isFirstVisit() {
    try { return !localStorage.getItem(VISITED_KEY); } catch { return false; }
}
export function markVisited() {
    try { localStorage.setItem(VISITED_KEY, "1"); } catch { }
}

export function isPhoneScreen() {
    return window.matchMedia("(max-width: 640px)").matches;
}

// ---------- helpers ----------
const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
// Width taken by the floating panel on the left (desktop only)
const panelMargin = el => el.clientWidth <= 900 || el.closest(".sc-canvas")?.classList.contains("panel-collapsed") ? 0 : 300;
const isPhone = el => el.clientWidth < 640;
const initials = name => {
    const p = String(name).split(" ").filter(x => x.length > 2 || /^[A-ZÀ-Ú]/.test(x));
    return ((p[0]?.[0] ?? "") + (p.length > 1 ? p[p.length - 1][0] : "")).toUpperCase();
};
const avatar = (n, cls = "") => n.photo
    ? `<div class="ft-avatar ${cls}"><img src="${esc(n.photo)}" alt="" loading="lazy" /></div>`
    : `<div class="ft-avatar ft-avatar-${n.sex === "F" ? "f" : "m"} ${cls}"><span>${esc(initials(n.name))}</span></div>`;
const sexClass = n => n.sex === "F" ? "ft-f" : "ft-m";

function createStage(el) {
    el.innerHTML = "";
    const viewport = document.createElement("div");
    viewport.className = "ft-viewport";
    const stage = document.createElement("div");
    stage.className = "ft-stage";
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.classList.add("ft-links");
    stage.append(svg);
    viewport.append(stage);
    el.append(viewport);

    let t = d3.zoomIdentity;
    const zoom = d3.zoom().scaleExtent([0.15, 2.5])
        .filter(e => !e.target.closest?.(".ft-toggle") && (!e.button || e.type === "wheel"))
        .on("zoom", e => { t = e.transform; stage.style.transform = `translate(${t.x}px,${t.y}px) scale(${t.k})`; });
    const sel = d3.select(viewport).call(zoom).on("dblclick.zoom", null);

    const api = {
        viewport, stage, svg, zoom,
        get transform() { return t; },
        goTo(x, y, k, dur = 750) {
            const w = viewport.clientWidth, h = viewport.clientHeight;
            const target = d3.zoomIdentity.translate(w / 2 - x * k, h / 2 - y * k).scale(k);
            (dur ? sel.transition().duration(dur).ease(d3.easeCubicInOut) : sel).call(zoom.transform, target);
        },
        fit(b, dur = 750, max = 1) {
            const m = panelMargin(viewport), w = viewport.clientWidth - m, h = viewport.clientHeight;
            const k = Math.min(max, 0.92 * Math.min(w / Math.max(b.w, 1), h / Math.max(b.h, 1)));
            api.goTo(b.x + b.w / 2 - m / 2 / k, b.y + b.h / 2, k, dur);
        },
        zoomIn() { sel.transition().duration(300).call(zoom.scaleBy, 1.3); },
        zoomOut() { sel.transition().duration(300).call(zoom.scaleBy, 1 / 1.3); },
        destroy() { sel.on(".zoom", null); el.innerHTML = ""; }
    };
    return api;
}

// =====================================================================
// TREEGRAPH / "Linhagem" (Highcharts Treegraph style: bezier + animations)
// =====================================================================
const COLORS = ["#2caffe", "#544fc5", "#00e272", "#fe6a35", "#6b8abc", "#d568fb", "#2ee0ca", "#fa4b42", "#feb56a", "#91e8e1"];

export function treegraph(el, data, dotnet) {
    destroy();
    el.innerHTML = "";
    const phone = isPhone(el);
    const dx = 54, dy = phone ? 190 : 250, dur = 650;
    const root = d3.hierarchy(data);
    root.x0 = 0; root.y0 = 0;
    root.descendants().forEach((d, i) => {
        d.id = i;
        d._children = d.children;
        // Desktop shows everything; phones start compact (root, parents, grandparents) and expand on tap
        if (phone && d.depth >= 2) d.children = null;
    });

    const svg = d3.select(el).append("svg").attr("class", "ft-tg").attr("width", "100%").attr("height", "100%");
    const g = svg.append("g");
    const gLink = g.append("g").attr("class", "ft-tg-links");
    const gNode = g.append("g").attr("class", "ft-tg-nodes");
    const zoom = d3.zoom().scaleExtent([0.2, 2.5]).on("zoom", e => g.attr("transform", e.transform));
    svg.call(zoom).on("dblclick.zoom", null);

    const tree = d3.tree().nodeSize([dx, dy]).separation((a, b) => a.parent === b.parent ? 1 : 1.25);
    const GROUP_COLOR = "#f5b33c";
    // The whole "Irmãos" branch (siblings, nephews...) shares one color
    const color = d => d.ancestors().some(a => a.data.id === 0) ? GROUP_COLOR : COLORS[d.depth % COLORS.length];
    const diagonal = d3.linkHorizontal().x(d => d.y).y(d => d.x);
    const hiddenCount = d => d._children ? d3.hierarchy(d.data).descendants().length - 1 : 0;

    function update(source) {
        tree(root);
        const nodes = root.descendants().reverse(), links = root.links();
        const t = svg.transition().duration(dur).ease(d3.easeCubicInOut);

        const node = gNode.selectAll("g.ft-tg-node").data(nodes, d => d.id);
        const enter = node.enter().append("g").attr("class", "ft-tg-node")
            .attr("transform", `translate(${source.y0},${source.x0})`).attr("opacity", 0);

        enter.append("circle").attr("class", "ft-tg-halo").attr("r", 16);
        enter.append("circle").attr("class", "ft-tg-dot").attr("r", 10)
            .on("click", (e, d) => {
                e.stopPropagation();
                if (!d._children) return;
                d.children = d.children ? null : d._children;
                update(d);
            });
        enter.append("text").attr("class", "ft-tg-count").attr("dy", "0.35em").attr("text-anchor", "middle");
        const label = enter.append("g").attr("class", "ft-tg-label").attr("transform", "translate(18,0)")
            .on("click", (e, d) => {
                // The "Irmãos" group (id 0) just toggles; people open the drawer
                if (d.data.id === 0) { d.children = d.children ? null : d._children; update(d); }
                else dotnet.invokeMethodAsync("OpenDetails", d.data.id);
            });
        label.append("text").attr("class", "ft-tg-name").attr("dy", "-0.15em").text(d => d.data.name);
        label.append("text").attr("class", "ft-tg-sub").attr("dy", "1.15em")
            .text(d => [d.data.role, d.data.years, d.data.place?.split(",")[0], d.data.spouses ? `⚭ ${d.data.spouses}` : null].filter(Boolean).join("  ·  "));

        const merged = node.merge(enter);
        merged.transition(t).attr("transform", d => `translate(${d.y},${d.x})`).attr("opacity", 1);
        merged.select(".ft-tg-dot")
            .attr("stroke", color)
            .attr("fill", d => d._children && !d.children ? color(d) : "#fff")
            .classed("ft-tg-clickable", d => !!d._children)
            .classed("ft-tg-deceased", d => !!d.data.isDeceased);
        merged.select(".ft-tg-halo").attr("fill", color);
        merged.classed("ft-tg-group", d => d.data.id === 0);
        merged.select(".ft-tg-count").text(d => d._children && !d.children ? hiddenCount(d) : "");

        node.exit().transition(t).remove()
            .attr("transform", `translate(${source.y},${source.x})`).attr("opacity", 0);

        const link = gLink.selectAll("path").data(links, d => d.target.id);
        const linkEnter = link.enter().append("path")
            .attr("stroke", d => color(d.target))
            .attr("d", () => { const o = { x: source.x0, y: source.y0 }; return diagonal({ source: o, target: o }); });
        link.merge(linkEnter).transition(t).attr("d", diagonal);
        link.exit().transition(t).remove()
            .attr("d", () => { const o = { x: source.x, y: source.y }; return diagonal({ source: o, target: o }); });

        root.eachBefore(d => { d.x0 = d.x; d.y0 = d.y; });
    }

    function bbox() {
        const ns = root.descendants();
        const x0 = d3.min(ns, d => d.x), x1 = d3.max(ns, d => d.x), y1 = d3.max(ns, d => d.y);
        return { x: -40, y: x0 - 40, w: y1 + 320, h: x1 - x0 + 80 };
    }
    function fit(d = 750) {
        const b = bbox(), m = panelMargin(el);
        const best = Math.min(1.2, 0.95 * Math.min((el.clientWidth - m) / b.w, el.clientHeight / b.h));
        // Never shrink below a readable size on phones: align the root to the left instead
        const k = phone ? Math.max(best, 0.55) : best;
        const tr = k > best
            ? d3.zoomIdentity.translate(16 - b.x * k, el.clientHeight / 2 + 40).scale(k)
            : d3.zoomIdentity.translate(m + (el.clientWidth - m) / 2 - (b.x + b.w / 2) * k, el.clientHeight / 2 - (b.y + b.h / 2) * k).scale(k);
        (d ? svg.transition().duration(d) : svg).call(zoom.transform, tr);
    }

    update(root);
    fit(0);
    current = {
        destroy: () => { svg.on(".zoom", null); el.innerHTML = ""; },
        zoomIn: () => svg.transition().duration(300).call(zoom.scaleBy, 1.3),
        zoomOut: () => svg.transition().duration(300).call(zoom.scaleBy, 1 / 1.3),
        fit: () => fit(),
        expandAll: () => { root.descendants().forEach(d => { if (d._children) d.children = d._children; }); update(root); setTimeout(fit, dur); },
        collapseAll: () => { root.descendants().forEach(d => { if (d.depth >= 1 && d._children) d.children = null; }); update(root); setTimeout(fit, dur); }
    };
}

// =====================================================================
// FAMILY TREE / "Árvore familiar": every relative laid out by generation,
// OrgChart-style cards with ▲ parents, ▼ children and ◀▶ siblings collapse
// =====================================================================
// Card size and spacing (same cards as the OrgChart-style view)
const F = { W: 196, H: 100, GAP_COUPLE: 34, GAP_UNIT: 52, VGAP: 96 };

// Unit order in each generation, minimizing what makes the chart hard to read:
// line crossings between families and long connectors (children far from their parents).
// Barycenter sweeps give a first order; "sifting" then moves every unit to its best position
// (and flips couple chains) until nothing improves.
function orderUnits(levels, nodes, parentsOf, gen, { skipBarycenter = false, frozen = new Set() } = {}) {
    const slot = new Map();
    const index = lv => { let i = 0; for (const u of lv) for (const m of u.members) slot.set(m, i++); };
    levels.forEach(index);
    const levelOf = new Map(levels.map((lv, i) => [lv[0].g, i]));

    // families[i] = { parents, children } whose children are on level i and parents on level i - 1
    const families = levels.map(() => new Map());
    for (const n of nodes) {
        const ps = parentsOf(n).filter(p => gen.get(p) === gen.get(n.id) - 1);
        if (!ps.length) continue;
        const fams = families[levelOf.get(gen.get(n.id))], key = ps.join("|");
        if (!fams.has(key)) fams.set(key, { parents: ps, children: [] });
        fams.get(key).children.push(n.id);
    }
    const famList = families.map(f => [...f.values()]);

    const LENGTH_WEIGHT = 0.15;
    const gapCost = i => {
        if (i <= 0 || i >= levels.length) return 0;
        // one edge per child, starting from the middle of its parents
        const edges = [];
        famList[i].forEach((f, fi) => {
            const anchor = d3.mean(f.parents, p => slot.get(p));
            for (const c of f.children) edges.push([anchor, slot.get(c), fi]);
        });
        let cost = 0;
        for (let a = 0; a < edges.length; a++) {
            cost += LENGTH_WEIGHT * Math.abs(edges[a][0] - edges[a][1]);
            for (let b = a + 1; b < edges.length; b++)
                if (edges[a][2] !== edges[b][2] && (edges[a][0] - edges[b][0]) * (edges[a][1] - edges[b][1]) < 0) cost += 1;
        }
        return cost;
    };
    const total = () => d3.sum(levels, (_, i) => gapCost(i));
    const local = i => gapCost(i) + gapCost(i + 1);

    // 1) Barycenter sweeps
    const barycenter = (i, down) => {
        const lv = levels[i];
        const score = new Map(lv.map(u => {
            const vs = down
                ? u.members.flatMap(m => parentsOf(nodes.find(n => n.id === m)).filter(p => levelOf.get(gen.get(p)) === i - 1))
                : (famList[i + 1] ?? []).filter(f => f.parents.some(p => u.members.includes(p))).flatMap(f => f.children);
            return [u, vs.length ? d3.mean(vs, v => slot.get(v)) : slot.get(u.members[0])];
        }));
        lv.sort((a, b) => score.get(a) - score.get(b));
        index(lv);
    };
    for (let it = 0; it < (skipBarycenter ? 0 : 8); it++) {
        if (it % 2 === 0) for (let i = 1; i < levels.length; i++) barycenter(i, true);
        else for (let i = levels.length - 2; i >= 0; i--) barycenter(i, false);
    }

    // 2) Sifting: try every unit in every position (and both chain directions)
    const sift = i => {
        const lv = levels[i];
        let improved = false;
        for (const u of [...lv].sort((a, b) => b.members.length - a.members.length)) {
            const from = lv.indexOf(u);
            let best = { cost: local(i), pos: from, flip: false };
            lv.splice(from, 1);
            for (let pos = 0; pos <= lv.length; pos++) {
                for (const flip of u.members.length > 1 && !frozen.has(u) ? [false, true] : [false]) {
                    if (flip) u.members.reverse();
                    lv.splice(pos, 0, u); index(lv);
                    const cost = local(i);
                    if (cost < best.cost - 1e-9) best = { cost, pos, flip };
                    lv.splice(pos, 1);
                    if (flip) u.members.reverse();
                }
            }
            if (best.flip) u.members.reverse();
            lv.splice(best.pos, 0, u); index(lv);
            if (best.pos !== from || best.flip) improved = true;
        }
        return improved;
    };
    let before = total();
    for (let round = 0; round < 10; round++) {
        let changed = false;
        for (let i = 0; i < levels.length; i++) changed = sift(i) || changed;
        for (let i = levels.length - 1; i >= 0; i--) changed = sift(i) || changed;
        const after = total();
        if (!changed || after >= before) break;
        before = after;
    }
}

// Distinct, soft colors so each family's connectors can be followed by eye
const LINE_COLORS = ["#6d5efc", "#0ea5e9", "#10b981", "#f59e0b", "#e2507a", "#8b5cf6", "#14b8a6", "#f97316", "#64748b", "#84cc16"];
const LANE_GAP = 16;
const RADIUS = 10;

// Parent anchor -> bus line -> child top, with rounded corners
function elbow(ax, ay, busY, cx, cy) {
    const dx = cx - ax;
    if (Math.abs(dx) < 1) return `M${ax},${ay}V${cy}`;
    const r = Math.min(RADIUS, Math.abs(dx) / 2, Math.abs(busY - ay), Math.abs(cy - busY));
    const sx = Math.sign(dx);
    return `M${ax},${ay}V${busY - r}Q${ax},${busY} ${ax + sx * r},${busY}H${cx - sx * r}Q${cx},${busY} ${cx},${busY + r}V${cy}`;
}

// Crossings when family "a" has its bus line above family "b" (same gap between two generations):
// b's trunk passes a's bus, and a's drops to its children pass b's bus.
function laneCrossings(a, b) {
    const inside = (x, f) => x > f.min + 0.5 && x < f.max - 0.5;
    return (inside(b.ax, a) ? 1 : 0) + a.cs.filter(c => inside(c.x, b)).length;
}

// Top-to-bottom lane order with the fewest crossings (exhaustive for small groups)
function bestLaneOrder(items) {
    if (items.length === 1) return items;
    const cost = order => {
        let c = 0;
        for (let i = 0; i < order.length; i++) for (let j = i + 1; j < order.length; j++) c += laneCrossings(order[i], order[j]);
        return c;
    };
    if (items.length > 7) return [...items].sort((a, b) => (b.max - b.min) - (a.max - a.min));
    let best = items, bestCost = Infinity;
    const permute = (rest, acc) => {
        if (!rest.length) { const c = cost(acc); if (c < bestCost) { bestCost = c; best = acc; } return; }
        rest.forEach((f, i) => permute([...rest.slice(0, i), ...rest.slice(i + 1)], [...acc, f]));
    };
    permute(items, []);
    return best;
}

function familyLayout(nodes, rootId, budgetMs = 450) {
    const byId = new Map(nodes.map(n => [n.id, n]));
    const childrenOf = new Map(nodes.map(n => [n.id, []]));
    const partnersOf = new Map(nodes.map(n => [n.id, new Set()]));
    const parentsOf = n => [n.fatherId, n.motherId].filter(id => id != null && byId.has(id));
    for (const n of nodes) {
        const ps = parentsOf(n);
        ps.forEach(p => childrenOf.get(p).push(n.id));
        if (ps.length === 2) { partnersOf.get(ps[0]).add(ps[1]); partnersOf.get(ps[1]).add(ps[0]); }
    }
    // Registered spouses (couples without children in common)
    for (const n of nodes) if (n.spouseId != null && byId.has(n.spouseId) && n.spouseId !== n.id) {
        partnersOf.get(n.id).add(n.spouseId); partnersOf.get(n.spouseId).add(n.id);
    }

    // Generations via BFS from the root (parents = -1, children = +1, partners = same)
    const gen = new Map(), visitOrder = [];
    const bfs = start => {
        gen.set(start, gen.get(start) ?? 0);
        const queue = [start];
        while (queue.length) {
            const id = queue.shift(), n = byId.get(id), g = gen.get(id);
            visitOrder.push(id);
            const next = [...[...partnersOf.get(id)].map(x => [x, g]), ...parentsOf(n).map(x => [x, g - 1]), ...childrenOf.get(id).map(x => [x, g + 1])];
            for (const [v, gv] of next) if (!gen.has(v)) { gen.set(v, gv); queue.push(v); }
        }
    };
    if (byId.has(rootId)) bfs(rootId);
    for (const n of nodes) if (!gen.has(n.id)) bfs(n.id);

    // Units = chains of couples in the same generation (e.g. Sofia — Lucas — Marina — Rafael)
    const unitOf = new Map(), units = [];
    for (const id of visitOrder) {
        if (unitOf.has(id)) continue;
        const g = gen.get(id), comp = new Set([id]), stack = [id];
        while (stack.length) for (const p of partnersOf.get(stack.pop())) if (gen.get(p) === g && !comp.has(p)) { comp.add(p); stack.push(p); }
        const degree = x => [...partnersOf.get(x)].filter(p => comp.has(p)).length;
        const start = [...comp].sort((a, b) => degree(a) - degree(b))[0];
        const members = [], seen = new Set();
        const walk = x => { seen.add(x); members.push(x); for (const p of partnersOf.get(x)) if (comp.has(p) && !seen.has(p)) { walk(p); break; } };
        walk(start);
        for (const x of comp) if (!seen.has(x)) members.push(x);
        const u = { members, g, x: 0, w: members.length * F.W + (members.length - 1) * F.GAP_COUPLE };
        members.forEach(m => unitOf.set(m, u));
        units.push(u);
    }

    const levels = d3.groups(units, u => u.g).sort((a, b) => a[0] - b[0]).map(x => x[1]);
    const offset = m => unitOf.get(m).members.indexOf(m) * (F.W + F.GAP_COUPLE);
    const cx = m => unitOf.get(m).x + offset(m) + F.W / 2;
    const pack = lv => { let x = -(d3.sum(lv, u => u.w) + F.GAP_UNIT * (lv.length - 1)) / 2; for (const u of lv) { u.x = x; x += u.w + F.GAP_UNIT; } };

    const desiredX = u => {
        let sum = 0, weight = 0;
        // From above: center each member under its parents
        for (const m of u.members) {
            const ps = parentsOf(byId.get(m)).filter(p => gen.get(p) === u.g - 1);
            if (ps.length) { sum += d3.mean(ps, cx) - offset(m) - F.W / 2; weight++; }
        }
        // From below: center the couple over its children
        const seen = new Set();
        for (const m of u.members) for (const c of childrenOf.get(m)) {
            if (seen.has(c) || gen.get(c) !== u.g + 1) continue;
            seen.add(c);
            const ps = parentsOf(byId.get(c)).filter(p => unitOf.get(p) === u);
            sum += cx(c) - d3.mean(ps, p => offset(p) + F.W / 2); weight++;
        }
        return weight ? sum / weight : null;
    };

    // Best positions keeping order and spacing (isotonic regression / PAVA)
    const solve = lv => {
        const d = lv.map(desiredX);
        let s = 0;
        const blocks = [];
        lv.forEach((u, i) => {
            const target = (d[i] ?? u.x) - s, w = d[i] == null ? 0.05 : 1;
            blocks.push({ v: target, w, n: 1 });
            while (blocks.length > 1 && blocks.at(-2).v > blocks.at(-1).v) {
                const b = blocks.pop(), a = blocks.pop();
                blocks.push({ v: (a.v * a.w + b.v * b.w) / (a.w + b.w), w: a.w + b.w, n: a.n + b.n });
            }
            s += u.w + F.GAP_UNIT;
        });
        let i = 0; s = 0;
        for (const b of blocks) for (let k = 0; k < b.n; k++, i++) { lv[i].x = b.v + s; s += lv[i].w + F.GAP_UNIT; }
    };
    const gMin = levels[0]?.[0]?.g ?? 0;
    const pos = new Map();
    // Coordinates for the current order: pack, then pull every unit towards its parents/children
    const place = () => {
        levels.forEach(pack);
        for (let it = 0; it < 40; it++) (it % 2 ? [...levels].reverse() : levels).forEach(solve);
        for (const u of units) u.members.forEach(m => pos.set(m, { x: u.x + offset(m), y: (u.g - gMin) * (F.H + F.VGAP) }));
    };
    // Each family = parents' anchor + its children, grouped by the gap they live in
    const buildFamilies = () => d3.groups(
        d3.groups(nodes.filter(n => parentsOf(n).length), n => parentsOf(n).join("|")).map(([key, kids]) => {
            const ps = key.split("|").map(Number), pp = ps.map(p => pos.get(p));
            const isCouple = ps.length === 2 && unitOf.get(ps[0]) === unitOf.get(ps[1]) && Math.abs(pp[0].x - pp[1].x) <= F.W + F.GAP_COUPLE + 1;
            const ax = d3.mean(pp, p => p.x + F.W / 2);
            const sy = isCouple ? pp[0].y + F.H / 2 : d3.max(pp, p => p.y + F.H);
            const cs = kids.map(c => ({ id: c.id, x: pos.get(c.id).x + F.W / 2, y: pos.get(c.id).y }));
            const min = Math.min(ax, ...cs.map(c => c.x)), max = Math.max(ax, ...cs.map(c => c.x));
            return { ax, sy, cs, min, max, ty: d3.min(cs, c => c.y), parents: ps };
        }), f => f.ty).map(([, fams]) => fams);
    // Families whose bus lines share horizontal space (they need separate lanes)
    const laneGroups = fams => {
        const sorted = [...fams].sort((a, b) => a.min - b.min), groups = [];
        for (const f of sorted) {
            const g = groups.at(-1);
            if (g && f.min < g.max + 12) { g.items.push(f); g.max = Math.max(g.max, f.max); }
            else groups.push({ items: [f], max: f.max });
        }
        return groups;
    };
    const laneOrderCost = order => {
        let c = 0;
        for (let i = 0; i < order.length; i++) for (let j = i + 1; j < order.length; j++) c += laneCrossings(order[i], order[j]);
        return c;
    };
    // What the eye actually sees: real line crossings (after picking the best lanes) and connector length
    const drawnCost = () => {
        place();
        let crossings = 0, length = 0;
        for (const fams of buildFamilies()) {
            for (const g of laneGroups(fams)) crossings += laneOrderCost(bestLaneOrder(g.items));
            for (const f of fams) length += f.max - f.min;
        }
        return crossings + length / 4000;
    };

    orderUnits(levels, nodes, parentsOf, gen);
    const started = performance.now(), BUDGET_MS = budgetMs;
    const snapshot = () => levels.map(lv => lv.map(u => [u, [...u.members]]));
    const restore = snap => snap.forEach((lv, i) => { levels[i].splice(0, levels[i].length, ...lv.map(([u, m]) => { u.members = [...m]; return u; })); });

    // Big moves: flip each couple chain (e.g. Manoel — Maria — Olivio) and re-arrange around it
    let best = drawnCost(), bestSnap = snapshot();
    for (const u of units.filter(x => x.members.length > 1)) {
        if (best < 1e-6 || performance.now() - started > BUDGET_MS / 2) break;
        restore(bestSnap);
        u.members.reverse();
        orderUnits(levels, nodes, parentsOf, gen, { skipBarycenter: true, frozen: new Set([u]) });
        const cost = drawnCost();
        if (cost < best - 1e-9) { best = cost; bestSnap = snapshot(); }
    }
    restore(bestSnap);

    // Fine moves against the drawn result: each unit to every position (and flip chains)
    for (let round = 0; round < 4 && best >= 1e-6; round++) {
        let improved = false;
        for (const lv of levels) for (const u of [...lv]) {
            if (performance.now() - started > BUDGET_MS) break;
            const from = lv.indexOf(u);
            let pick = { cost: best, pos: from, flip: false };
            lv.splice(from, 1);
            for (let p = 0; p <= lv.length; p++) for (const flip of u.members.length > 1 ? [false, true] : [false]) {
                if (p === from && !flip) continue;
                if (flip) u.members.reverse();
                lv.splice(p, 0, u);
                const cost = drawnCost();
                if (cost < pick.cost - 1e-9) pick = { cost, pos: p, flip };
                lv.splice(p, 1);
                if (flip) u.members.reverse();
            }
            if (pick.flip) u.members.reverse();
            lv.splice(pick.pos, 0, u);
            if (pick.cost < best - 1e-9) { best = pick.cost; improved = true; }
        }
        if (!improved) break;
    }
    place();

    // Links: couple (horizontal) + family -> children (orthogonal with a bus line)
    const links = [];
    for (const u of units) for (let i = 1; i < u.members.length; i++) {
        const a = pos.get(u.members[i - 1]), b = pos.get(u.members[i]);
        if (partnersOf.get(u.members[i - 1]).has(u.members[i]))
            links.push({ type: "couple", d: `M${a.x + F.W},${a.y + F.H / 2}H${b.x}` });
    }
    let colorIndex = 0;
    for (const fams of buildFamilies()) {
        // The lane order is chosen so trunks and drops don't cut through other buses
        for (const g of laneGroups(fams)) {
            const order = bestLaneOrder(g.items);
            order.forEach((f, i) => { f.busY = f.ty - F.VGAP / 2 + (i - (order.length - 1) / 2) * LANE_GAP; });
        }
        for (const f of [...fams].sort((a, b) => a.ax - b.ax)) {
            const d = f.cs.map(c => elbow(f.ax, f.sy, f.busY, c.x, c.y)).join("");
            links.push({ type: "children", d, parents: f.parents, children: f.cs.map(c => c.id), color: LINE_COLORS[colorIndex++ % LINE_COLORS.length] });
        }
    }

    const all = [...pos.values()];
    const x0 = d3.min(all, p => p.x), y0 = d3.min(all, p => p.y);
    const bbox = { x: x0, y: y0, w: d3.max(all, p => p.x + F.W) - x0, h: d3.max(all, p => p.y + F.H) - y0 };
    return { pos, links, bbox, gen };
}

export function familyTree(el, nodes, focusId, dotnet) {
    destroy();
    const stage = createStage(el);
    const byId = new Map(nodes.map(n => [n.id, n]));
    const parentsOf = n => [n.fatherId, n.motherId].filter(id => id != null && byId.has(id));
    const childrenOf = new Map(nodes.map(n => [n.id, []]));
    const partnersOf = new Map(nodes.map(n => [n.id, new Set()]));
    for (const n of nodes) {
        const ps = parentsOf(n);
        ps.forEach(p => childrenOf.get(p).push(n.id));
        if (ps.length === 2) { partnersOf.get(ps[0]).add(ps[1]); partnersOf.get(ps[1]).add(ps[0]); }
    }
    // Registered spouses (couples without children in common)
    for (const n of nodes) if (n.spouseId != null && byId.has(n.spouseId) && n.spouseId !== n.id) {
        partnersOf.get(n.id).add(n.spouseId); partnersOf.get(n.spouseId).add(n.id);
    }
    const isSibling = (a, b) => a !== b && parentsOf(byId.get(a)).some(p => parentsOf(byId.get(b)).includes(p));
    const hasSiblings = id => parentsOf(byId.get(id)).some(p => childrenOf.get(p).length > 1);

    // Collapse state: ▲ parents, ▼ children, ◀▶ siblings
    const collapsedUp = new Set(), collapsedDown = new Set(), collapsedSide = new Set();
    const cards = new Map();
    let layout = null, firstRender = true;
    const dur = 500;

    // Visible people = reachable from the focus without crossing collapsed edges
    function visibleIds(up = collapsedUp, down = collapsedDown, side = collapsedSide) {
        const seen = new Set([focusId]), queue = [focusId];
        const blockedChild = (parent, child) =>
            // a collapsed parent also hides the children shared with its partners
            down.has(parent) || parentsOf(byId.get(child)).some(p => p !== parent && down.has(p))
            // a person with collapsed siblings hides the parents' other children
            || [...side].some(s => s !== child && parentsOf(byId.get(s)).includes(parent) && isSibling(s, child));
        while (queue.length) {
            const id = queue.shift(), n = byId.get(id), next = [];
            if (!up.has(id)) next.push(...parentsOf(n));
            next.push(...childrenOf.get(id).filter(c => !blockedChild(id, c)));
            if (!down.has(id)) next.push(...partnersOf.get(id));
            for (const v of next) if (!seen.has(v)) { seen.add(v); queue.push(v); }
        }
        return seen;
    }
    // Number of people a toggle hides/shows (0 = no toggle needed)
    function toggleEffect(id, set) {
        const copy = s => s === set ? new Set(s) : s;
        const up = copy(collapsedUp), down = copy(collapsedDown), side = copy(collapsedSide);
        const target = set === collapsedUp ? up : set === collapsedDown ? down : side;
        target.has(id) ? target.delete(id) : target.add(id);
        return Math.abs(visibleIds().size - visibleIds(up, down, side).size);
    }

    const svg = d3.select(stage.svg);

    function render(anchorId) {
        const visible = visibleIds();
        const visibleNodes = nodes.filter(n => visible.has(n.id));
        const before = anchorId != null ? layout?.pos.get(anchorId) : null;
        // Full search on first paint; a shorter one when toggling so the arrows feel instant
        layout = familyLayout(visibleNodes, focusId, firstRender ? 450 : 200);
        const { pos, links, bbox, gen } = layout;

        svg.attr("width", bbox.w + 400).attr("height", bbox.h + 400)
            .style("left", `${bbox.x - 200}px`).style("top", `${bbox.y - 200}px`)
            .attr("viewBox", `${bbox.x - 200} ${bbox.y - 200} ${bbox.w + 400} ${bbox.h + 400}`);
        svg.selectAll("g").remove();
        // Each connector = background-colored halo + colored line, drawn one family at a time,
        // so where two families must cross, the line on top shows a small gap ("bridge")
        const groups = svg.selectAll("g").data(links).join("g");
        groups.filter(d => d.type === "children").append("path").attr("class", "ft-link-halo").attr("d", d => d.d);
        groups.append("path").attr("class", d => `ft-link ft-link-${d.type}`).attr("d", d => d.d)
            .style("stroke", d => d.color ?? null);

        // Keep the toggled card at the same place on screen
        if (before) {
            const after = pos.get(anchorId), t = stage.transform;
            stage.goTo((stage.viewport.clientWidth / 2 - t.x) / t.k + after.x - before.x,
                (stage.viewport.clientHeight / 2 - t.y) / t.k + after.y - before.y, t.k, dur);
        }

        const g0 = gen.get(focusId) ?? 0;
        for (const [id, c] of cards) if (!visible.has(id)) {
            cards.delete(id);
            c.classList.add("ft-leaving");
            setTimeout(() => c.remove(), 400);
        }
        for (const n of visibleNodes) {
            let c = cards.get(n.id);
            if (!c) {
                c = createCard(n);
                c.style.animationDelay = firstRender ? `${Math.min(Math.abs((gen.get(n.id) ?? 0) - g0) * 90, 700)}ms` : "0ms";
                stage.stage.append(c);
                cards.set(n.id, c);
            }
            const p = pos.get(n.id);
            c.style.left = `${p.x}px`;
            c.style.top = `${p.y}px`;
            addToggles(c, n);
        }
        firstRender = false;
    }

    function createCard(n) {
        const c = document.createElement("div");
        c.className = `ft-oc-node ft-graph-node ${sexClass(n)} ${n.isDeceased ? "ft-deceased" : ""} ${n.id === focusId ? "ft-oc-focus" : ""}`;
        c.dataset.id = n.id;
        c.innerHTML = `
            <div class="ft-oc-title">${avatar(n, "ft-avatar-sm")}<span title="${esc(n.name)}">${esc(n.name)}</span></div>
            <div class="ft-oc-content">
                <b title="${esc(n.relationship ?? "")}">${esc(n.relationship ?? "")}</b>
                <span>${n.isDeceased ? '<i class="ft-cross">✝</i>' : ""}${esc(n.years || "—")}</span>
                ${n.place ? `<small title="${esc(n.place)}">${esc(n.place)}</small>` : ""}
            </div>`;
        c.addEventListener("click", e => { if (!e.target.closest(".ft-toggle")) dotnet.invokeMethodAsync("OpenDetails", n.id); });
        c.addEventListener("dblclick", () => { if (n.id !== focusId) dotnet.invokeMethodAsync("Refocus", n.id); });
        c.addEventListener("mouseenter", () => highlightLinks(n.id, true));
        c.addEventListener("mouseleave", () => highlightLinks(n.id, false));
        return c;
    }

    function addToggles(c, n) {
        c.querySelectorAll(".ft-toggle").forEach(b => b.remove());
        const toggles = [["up", collapsedUp, "pais"], ["down", collapsedDown, "filhos"]];
        if (hasSiblings(n.id)) toggles.push(["left", collapsedSide, "irmãos"], ["right", collapsedSide, "irmãos"]);
        for (const [dir, set, label] of toggles) {
            const collapsed = set.has(n.id), count = toggleEffect(n.id, set);
            if (!collapsed && count === 0) continue;
            const b = document.createElement("button");
            b.className = `ft-toggle ft-toggle-${dir} ${collapsed ? "" : "open"}`;
            b.title = `${collapsed ? "Mostrar" : "Recolher"} ${label} (${count} pessoas)`;
            if (collapsed && dir !== "left") b.dataset.count = `+${count}`;
            b.addEventListener("click", e => {
                e.stopPropagation();
                collapsed ? set.delete(n.id) : set.add(n.id);
                render(n.id);
            });
            c.append(b);
        }
    }

    function highlightLinks(id, on) {
        const related = d => d.parents?.includes(id) || d.children?.includes(id);
        svg.selectAll("path.ft-link")
            .classed("ft-link-active", d => on && related(d))
            .classed("ft-link-dim", d => on && !related(d));
    }

    render(null);
    const p0 = layout.pos.get(focusId);
    stage.goTo(p0.x + F.W / 2, p0.y + F.H / 2, isPhone(el) ? 0.6 : 0.85, 0);
    current = {
        destroy: stage.destroy, zoomIn: stage.zoomIn, zoomOut: stage.zoomOut,
        fit: () => stage.fit(layout.bbox),
        expandAll: () => { collapsedUp.clear(); collapsedDown.clear(); collapsedSide.clear(); render(null); }
    };
}

