#!/bin/sh
# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

set -e
git config --local core.hooksPath .githooks
echo "Pre-commit hook enabled for this repository."
