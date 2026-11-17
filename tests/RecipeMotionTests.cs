using System;
using OC2HudCustomization;
using UnityEngine;

internal static partial class BehaviorTests
{
    private static void Motion()
    {
        Vector2 entry = new Vector2(200f, 550f), target = new Vector2(0f, 296f);
        foreach (float dt in new float[] { 1f / 30f, 1f / 60f, 1f / 144f })
        {
            RecipeMotion motion = new RecipeMotion(true);
            Vector2 position = motion.Step(target, entry, 0f, 0f, dt);
            Check(position == entry && motion.BranchOpacity == 0f, "new card shows head only at first-row entry");
            position = motion.Step(target, entry, 1f, .1f, dt);
            position = motion.Step(target, entry, .21f, .45f, dt);
            Check(position == entry, "first eighty percent of horizontal entrance holds row one");
            position = motion.Step(target, entry, .2f, .46f, dt);
            Check(position == entry, "horizontal tail is the exact descent origin");
            float previousY = position.y;
            int frames = (int)Math.Ceiling(.21f / dt);
            for (int frame = 1; frame < frames; frame++)
            {
                float progress = .46f + .14f * frame / frames;
                position = motion.Step(target, entry, .2f * (1f - (float)frame / frames), progress, dt);
                Check(position.y <= previousY + .0001f && position.y > target.y,
                    "quick descent follows native progress without overshoot");
                Check(motion.BranchOpacity == 0f, "ingredients remain hidden during descent");
                previousY = position.y;
            }
            position = motion.Step(target, entry, 0f, .60f, dt);
            Check(position == target && motion.BranchOpacity == 0f, "card lands before ingredient reveal");
            position = motion.Step(target, entry, 0f, .8f, dt);
            Check(position == target && motion.BranchOpacity > 0f && motion.BranchOpacity < 1f,
                "ingredients fade into native unfold only at destination");
            Check(motion.Step(target, entry, 0f, .85f, dt) == target && motion.BranchOpacity == 1f,
                "native main unfolding is fully visible after landing");
            position = motion.Step(target, entry, 0f, 1f, dt);
            Check(position == target && !motion.Entering, "native completion does not replay landing");
            Vector2 newTarget = new Vector2(100f, 470f);
            Check(motion.Step(newTarget, entry, 0f, 1f, 0f) == target, "settled retarget starts at current visual position");
            position = motion.Step(newTarget, entry, 0f, 1f, dt);
            Check(position.y > target.y && position.y < newTarget.y, "existing card reflows smoothly");
            motion.Reset();
            Check(motion.Step(target, entry, 0f, 0f, dt) == target && motion.BranchOpacity == 1f,
                "disable does not replay entrance or leave ingredients hidden");
        }
        RecipeMotion retarget = new RecipeMotion(true);
        retarget.Step(target, entry, 1f, .1f, .01f);
        retarget.Step(target, entry, .2f, .46f, .01f);
        Vector2 current = retarget.Step(target, entry, .1f, .5f, .01f);
        Vector2 other = new Vector2(-100f, 150f);
        Check(retarget.Step(other, entry, .1f, .5f, .01f) == current, "column change during landing preserves current position");
        Check(retarget.Step(other, entry, 0f, .65f, .01f) == other, "retarget arrives before branch reveal");
        current = retarget.Step(other, entry, 0f, .65f, 0f);
        Check(retarget.Step(other, entry, 0f, .65f, 1f) == current, "native pause freezes landing despite wall time");
        RecipeMotion skipped = new RecipeMotion(true);
        Check(skipped.Step(target, entry, 0f, 1.2f, .5f) == target, "long frame cannot leave a card hanging");
        RecipeMotion discovered = new RecipeMotion(false);
        Check(discovered.Step(target, entry, 1f, 0f, 0f) == target && discovered.BranchOpacity == 1f,
            "existing discovery preserves native presentation");
        RecipeMotion transient = new RecipeMotion(true);
        transient.Step(target, entry, 0f, 0f, .016f);
        Check(transient.Step(target, entry, 1f, .1f, .016f) == entry, "initial zero shader offset cannot release landing");
    }
}
