#!/usr/bin/env bash
set -e

# Fetch latest tags from remote
git fetch --tags

# Get the latest tag (sorted by version)
LATEST_TAG=$(git tag --sort=-v:refname | head -n 1)

# If no tag exists, start from 0.1.0
if [ -z "$LATEST_TAG" ]; then
  NEXT_TAG="0.1.0"
else
  IFS='.' read -r MAJOR MINOR PATCH <<<"$LATEST_TAG"
  PATCH=$((PATCH + 1))
  NEXT_TAG="$MAJOR.$MINOR.$PATCH"
fi

echo "Latest tag: ${LATEST_TAG:-none}"
echo "Next tag: $NEXT_TAG"

# Create tag
git tag "$NEXT_TAG"

# Push tag
git push origin "$NEXT_TAG"

echo "✅ Released version $NEXT_TAG"
