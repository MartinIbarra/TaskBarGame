# Maps

`Source/map1.psd` is the editable source for the first expedition map.

Unity runtime uses PNG files from `Assets/Resources/Maps`:

- `map1.png`: base map image.
- `map1_nodes.png`: optional transparent overlay with artist-authored node markers and labels.
- `map1_route_safety.png`: optional transparent overlay for the Safety route.
- `map1_route_loot.png`: optional transparent overlay for the Loot route.
- `map1_route_challenge.png`: optional transparent overlay for the Challenge route.

If an optional overlay is missing, the scene keeps using the editable generated
`Routes` and `Nodes` hierarchy as a fallback.
