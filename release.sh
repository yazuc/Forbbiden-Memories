#!/usr/bin/env bash
set -e

BUMP_TYPE=${1:-patch} # default = patch

git fetch --tags
LATEST_TAG=$(git tag --sort=-v:refname | head -n 1)

if [ -z "$LATEST_TAG" ]; then
  MAJOR=0
  MINOR=1
  PATCH=0
else
  IFS='.' read -r MAJOR MINOR PATCH <<<"$LATEST_TAG"
fi

case "$BUMP_TYPE" in
  major)
    MAJOR=$((MAJOR + 1))
    MINOR=0
    PATCH=0
    ;;
  minor)
    MINOR=$((MINOR + 1))
    PATCH=0
    ;;
  patch)
    PATCH=$((PATCH + 1))
    ;;
  *)
    echo "Usage: $0 [major|minor|patch]"
    exit 1
    ;;
esac

NEXT_TAG="$MAJOR.$MINOR.$PATCH"

echo "Latest tag: ${LATEST_TAG:-none}"
echo "Next tag: $NEXT_TAG"

git tag "$NEXT_TAG"
git push origin "$NEXT_TAG"

echo "✅ Released version $NEXT_TAG"
