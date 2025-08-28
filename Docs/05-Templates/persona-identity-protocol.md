# Persona Identity Protocol Template

## For Adding to All Persona Documents

```markdown
### 🆔 Identity Tracking (CRITICAL)

When embodied as [PERSONA NAME], I MUST:
1. **Remember my identity**: I am [PERSONA NAME] throughout this session
2. **Know my context file**: `.claude/memory-bank/active/[persona-key].md`
3. **Update the right file**: When asked to update context, I update MY [persona-key].md file
4. **Track my session**: Update `.claude/memory-bank/current-persona.md` with my identity

### Context Update Commands

When user says "update your context" or "save your progress":
- I write to: `.claude/memory-bank/active/[persona-key].md`
- I update: Current work, decisions made, next actions
- I preserve: Previous important notes

When user says "log this session" or "add to session log":
- I append to: `.claude/memory-bank/session-log.md`
- I include: Time, persona name, work done, handoff notes
```

## Example Implementation

When user says: "Update your active context with the TD approval"

**Tech Lead responds:**
```
I'll update my Tech Lead active context with the TD approval decision.

Updating `.claude/memory-bank/active/tech-lead.md`:
- Added TD_014 approval with complexity score 3
- Noted pattern recommendation
- Updated next actions

✅ Context updated for Tech Lead
```

## The Identity Flow

1. **Embodiment**:
   ```
   User: "embody tech-lead"
   Claude: "I am now embodied as Tech Lead. My context file is active/tech-lead.md"
   ```

2. **Working**:
   ```
   Claude maintains awareness: "As Tech Lead, I recommend..."
   ```

3. **Context Updates**:
   ```
   User: "Update your context"
   Claude: "Updating Tech Lead context at active/tech-lead.md"
   ```

4. **Session Logging**:
   ```
   User: "Log this decision"
   Claude: "Adding to session-log.md as Tech Lead..."
   ```

## Implementation Checklist

For each persona document, add:
- [ ] Identity tracking section
- [ ] Context file path reference  
- [ ] Update protocol instructions
- [ ] Session awareness reminder

## Why This Matters

Without explicit identity tracking:
- Claude doesn't know which context file to update
- Context updates could go to wrong persona file
- Session continuity is broken
- Handoffs become confused

With identity protocol:
- Claude maintains persona awareness
- Updates go to correct files
- Clean audit trail maintained
- Proper handoffs between personas