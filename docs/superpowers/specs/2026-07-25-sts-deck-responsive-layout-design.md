# STS Deck Responsive Layout Design

## Problem

The deck panel keeps a narrow, portrait-oriented viewport on desktop WebGL.
Although its grid is configured for multiple columns, cards outside the center
column are clipped instead of using the available 16:9 screen width.

## Expected behavior

- Desktop landscape displays deck cards in one centered horizontal row.
- Up to three cards fit without scrolling at the normal card size.
- Larger decks remain accessible through horizontal scrolling.
- Mobile and narrow portrait displays keep the existing vertical flow.
- Selecting a card keeps the existing centered preview behavior.
- The layout updates when the WebGL canvas dimensions change.

## Responsive rule

The desktop layout is active when the available viewport is landscape and at
least 900 pixels wide. It uses one fixed row, horizontal content sizing, and a
horizontal `ScrollRect`.

Below that breakpoint, the panel restores its serialized mobile constraint,
vertical content sizing, and vertical scrolling.

## Implementation boundaries

`DeckGridPanel` owns the responsive decision and applies it before measuring
the content. A pure layout resolver returns the orientation, row/column
constraint, and required content dimensions so edit-mode tests do not depend
on a running scene.

The existing STS Boot scene is widened for the deck panel and its scroll
viewport. No card prefab, card art, selection animation, game rule, or backend
behavior changes.

## Tests

- Landscape desktop resolves to one horizontal row.
- A three-card desktop deck fits without requiring overflow.
- A larger desktop deck produces horizontal overflow.
- Portrait and narrow viewports preserve the vertical layout.
- Existing STS edit-mode tests remain green.
