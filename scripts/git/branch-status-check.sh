#!/bin/bash
# Branch Status Check for AI Persona Embodiment
# Usage: source ./scripts/branch-status-check.sh

echo "🌿 Branch Status Analysis:"

# Get current branch
current_branch=$(git rev-parse --abbrev-ref HEAD)
echo "   Current Branch: $current_branch"

# Check if on main
if [ "$current_branch" = "main" ]; then
    echo "   ✅ On main branch - ready for new work"
    echo "   💡 Create feature branch for work items: git checkout -b feat/VS_XXX"
    exit 0
fi

# Check if branch has associated PR
echo "   🔍 Checking PR status..."

# Use GitHub CLI to check PR status
pr_info=$(gh pr view "$current_branch" --json state,merged,url,title 2>/dev/null)

if [ $? -eq 0 ]; then
    # PR exists - parse status
    pr_state=$(echo "$pr_info" | jq -r '.state')
    pr_merged=$(echo "$pr_info" | jq -r '.merged')
    pr_url=$(echo "$pr_info" | jq -r '.url')
    pr_title=$(echo "$pr_info" | jq -r '.title')
    
    echo "   📋 PR Found: $pr_title"
    echo "   🔗 URL: $pr_url"
    
    if [ "$pr_merged" = "true" ]; then
        echo "   ✅ PR MERGED - Branch cleanup recommended"
        echo ""
        echo "   🧹 Suggested Actions:"
        echo "      git checkout main"
        echo "      git pull origin main" 
        echo "      git branch -d $current_branch"
        echo "      git push origin --delete $current_branch"
        echo ""
        echo "   Would you like to clean up this merged branch? (y/n)"
        
    elif [ "$pr_state" = "OPEN" ]; then
        echo "   🟡 PR OPEN - Check before continuing work"
        echo ""
        echo "   🤔 Consider:"
        echo "      - Are you continuing work on this PR?"
        echo "      - Has PR received feedback requiring changes?"
        echo "      - Should this work be in a different branch?"
        echo ""
        
    elif [ "$pr_state" = "CLOSED" ]; then
        echo "   ❌ PR CLOSED (not merged) - Investigate why"
        echo ""
        echo "   🚨 Warning: This branch's PR was closed without merging"
        echo "   🤔 Possible reasons:"
        echo "      - Work was abandoned or superseded"
        echo "      - PR was rejected and needs rework" 
        echo "      - Work moved to different branch"
        echo ""
        echo "   💡 Suggestion: Review PR comments before continuing"
    fi
    
else
    # No PR found
    echo "   ⚪ No PR found for this branch"
    echo ""
    echo "   🤔 Consider:"
    echo "      - Is this work ready for PR? (gh pr create)"
    echo "      - Should this be merged back to main?"
    echo "      - Is this experimental work?"
    echo "      - Should you create a new branch instead?"
fi

# Check branch freshness
echo ""
echo "   🔄 Checking branch freshness..."
git fetch origin main --quiet 2>/dev/null

if git rev-parse --verify origin/main >/dev/null 2>&1; then
    behind_count=$(git rev-list --count HEAD..origin/main 2>/dev/null || echo "0")
    ahead_count=$(git rev-list --count origin/main..HEAD 2>/dev/null || echo "0")
    
    if [ "$behind_count" -gt 0 ]; then
        echo "   ⚠️  Branch is $behind_count commits behind main"
        if [ "$behind_count" -gt 10 ]; then
            echo "      🚨 STALE: Consider rebasing or creating fresh branch"
            echo "      git rebase origin/main"
        else
            echo "      💡 Consider updating: git rebase origin/main"
        fi
    fi
    
    if [ "$ahead_count" -gt 0 ]; then
        echo "   📈 Branch is $ahead_count commits ahead of main"
    fi
    
    if [ "$behind_count" -eq 0 ] && [ "$ahead_count" -eq 0 ]; then
        echo "   ✅ Branch is up to date with main"
    fi
else
    echo "   ⚠️  Could not fetch origin/main - check network"
fi

echo ""
echo "   📝 Ready to proceed with current branch? Review above analysis."