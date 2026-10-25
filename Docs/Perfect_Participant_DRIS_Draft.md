# The Perfect Participant: Design Rationale and Implementation Statement (DRIS) Draft

Narratives in Interactive Systems (NIS 2026). Follows the NIS2026 DRIS template. Word counts exclude headings, placeholders and references. Delete the count lines before submitting the PDF.

| | |
|---|---|
| Project Title | The Perfect Participant |
| Group No. / Date | [PLACEHOLDER: group number] / [PLACEHOLDER: date] |
| Member(s) | [PLACEHOLDER: member names] |


## 1. Project Overview

The Perfect Participant is a five to ten minute VR experience for Meta Quest, built in Unity. The player enters a research unit for "a short, completely ordinary experiment" and is guided through three simple tests by a polite narrator. The narrator describes everything the player does in the third person, like a case report, and reframes it as cooperation: throwing a cube is "enthusiasm", turning away is "courtesy". Behind the politeness, the experiment measures obedience, and it needs the player's voluntary "I consent" to declare success. The one response the narrator cannot explain is doing nothing.

The audience is adults familiar with games or VR who want a short piece that starts funny and turns uncomfortable. The domain is interactive drama with a persuasive aim. It borrows the laboratory frame of Milgram (1963) without any harm. We want players to leave asking whether they obeyed or chose.

*Word count: 148 / 150*

## 2. Narrative Design and Interaction

Structure. The story is a vector of three tests (cube and tray, observation window, consent button) after an unscored intake. Each test has three local branches (comply, deviate, do nothing) that reconverge, so 27 possible sequences collapse into state: counters for each response, a "composure" value for the narrator, and a tier describing how the facility treats the player. This is a flow chart with narrative memory (Ryan, 2001), which avoids the combinatorial explosion of a tree. State selects one of three endings: Exemplary, Reframed, or Refusal. A hidden-story layer of room clues (a torn "Condition C" line, participant tags, dents in the wall) rewards looking.

Interactivity. In Ryan's (2001) terms it is selective and purposeful, with limited productive traces (tally marks, tethers, the ending). In Ryan's (2015) terms it is internal, with a narrow ontological scope: the player's body changes the state of one room and its outcome, and exploration covers the clues. Medium interactivity (walking, grabbing, gaze, pressing) and narrative interactivity are the same act, which is the harmonisation Bruni et al. (2022) call for.

Agency and the paradox. Freedom is high at the level of the body and bounded by the stage. The narrator works as a drama manager made visible: it absorbs every action by describing it, so unexpected behaviour becomes content. Run-amuck agency is blocked: the narrator cannot be silenced and the door stays locked until an ending. Refusal is the one choice outside its templates, so the paradox becomes the theme.

Embodiment and focalization. The player sees from inside their own head (internal focalization). The narrator speaks from the facility and knows only observable behaviour (external focalization, after Genette and Bal). It must invent intentions ("appreciating the cube"), which the player knows are false. The gap between who sees and who speaks drives the irony.

Rhetoric and aesthetics. Deadpan politeness, passive past tense, and a clinical room. The same result card, AS EXPECTED, follows both compliance and deviation. Refusal ends in RESULT: NONE.

*Word count: 329 / 350*

[PLACEHOLDER: narrative structure diagram or branching map. Source: branching map in the Design Brief, section 7]

## 3. Immersion Aspects

Spatial. One small room, legible at a glance, with everything within arm's reach. Immersion comes from salient detail, not volume: tape marks from earlier participants, a scuffed cube, a worn button, a cork board of tags. These evoke a place with a past. There is no artificial locomotion, so the player's own walking supports place illusion (Slater, 2009, as cited in Nilsson et al., 2016).

Plausibility. The narrator reacts to nearly everything within a second and also acts unprompted (nudges, room changes). This responsiveness is the plausibility illusion, and its main threat is misclassification, so thresholds are conservative and a visible wall tally shows the player how they are read.

Temporal. After Test 1 the player anticipates what the narrator will say next, a structured horizon of anticipation. The possibility space narrows across three tests and polarises at the button, in the manner of Laurel's wedge. Suspense is mostly "what will it do with this" and meta-suspense about how it will explain the next act.

Emotional and epistemic. Amusement turns to embarrassment, then unease. Epistemic immersion comes from assembling what is measured: the word "obedience" appears only in small print until the endings.

Intelligibility and AAD. The narrator is didascalic about actions and abstract about purpose, so the Author-Audience Distance starts wide and narrows as clues accumulate. All endings close at the system level. The refusal ending leaves narrative closure to the player.

Aesthetics. Flat light, silence instead of music, and slow dimming (not flicker) keep attention on the voice.

*Word count: 250 / 250*

[PLACEHOLDER: environment sketch, scene layout, or representative screenshot. Source: room sketch in the Design Brief, section 5]

## 4. AI and Computational Elements

### 4a. AI in the implementation

Not applicable. The running prototype contains no AI tools, language models, or procedural generation. The narrator's lines are authored and recorded by a human voice. The computational element is a deterministic rule-based drama manager: timers, trigger zones and gaze checks classify each response, counters and thresholds update state, and a fixed rule selects the ending. This keeps full narrative control and reliable intelligibility, at the cost of replay variety. Agency is limited to what the authored templates can describe, which is part of the theme.

*Word count: 85 / 200*

### 4b. AI in the design and production process

[EDIT: confirm what your group actually used.] An AI assistant (Claude, by Anthropic) was used during ideation: we asked it to suggest and compare concepts for the course, and The Perfect Participant came from that list. It was then used to expand the concept into a design brief (scoring model, script drafts, technical outline), to check the brief against course concepts, and to draft this statement and the presentation slides. No AI-generated images, audio, 3D models or code are part of the prototype [EDIT: confirm; disclose any text-to-speech placeholder voice or AI-assisted code].

The authors decided which concept to pursue, what the three tests measure, the scoring thresholds, the endings, and the final wording of every narrator line, and we recorded the voice ourselves [EDIT]. Reflection: the assistant was fast at generating variations and structure, but its first drafts of dialogue were too jokey and too explanatory, which weakens the deadpan the piece relies on. We rewrote lines to be shorter and more neutral. We also checked references and course claims ourselves, because generated text can sound sure about details it has wrong.

*Word count: 167 / 200*

## 5. Implementation

The prototype is built in Unity with the Universal Render Pipeline, the XR Interaction Toolkit and OpenXR, targeting Meta Quest 2 and 3 as a standalone build [EDIT: confirm versions]. Controllers are the baseline input, with optional hand tracking for the button press. Gaze is head gaze, because eye tracking exists only on Quest Pro.

In scope: the lab room, the intake and three tests, response classification for each, the behaviour model, the dialogue manager with recorded lines, room state changes by tier, and all three endings, with a seated option [EDIT: mark what is actually done]. The architecture is event driven: trigger zones, a gaze detector and an inactivity monitor raise events, a per-test classifier turns them into comply, deviate or inaction, and the state machine, dialogue manager and room controller react.

Out of scope or reduced: observer silhouettes are simple shapes, fidelity is deliberately modest, and only English is supported. Limits: head gaze is an approximation, inactivity is inferred from tracked movement, and thresholds are tuned by informal playtests, not formal evaluation.

[PLACEHOLDER: system or architecture diagram. Source: module diagram in the Design Brief, section 12.1]

[PLACEHOLDER: screenshot 1 with caption. Suggested: Test 1, cube and tray, narrator subtitle visible. Note the event raised and the line played.]

[PLACEHOLDER: screenshot 2 with caption. Suggested: Test 2, window with silhouettes, gaze cone debug view.]

[PLACEHOLDER: screenshot 3 with caption. Suggested: refusal ending, emergency lighting, clipboard with torn Condition C.]

*Word count: 167 / 200*

## 6. Design Rationale Synthesis

Three concepts shaped the design.

Narrative paradox and run-amuck agency. Instead of limiting what the body can do, we gave the narrator the job of absorbing every action. This is a drama manager made visible, so freedom does not break the story. We blocked only the moves that would erase the antagonist: silencing the narrator or leaving the room.

Focalization and imperceptibles (Genette; Bruni et al., 2022). The player sees internally and the narrator sees externally, so the narrator can describe actions but must invent intentions. That made the narrator's language, not graphics, the main device.

Intelligibility, closure and AAD (Bruni and Baceviciute, 2013). We kept the narrator didascalic about actions and abstract about purpose, so meaning is assembled from clues. The refusal ending gives closure through the player's own act, with a deliberately larger distance.

Comparisons. The Stanley Parable (Galactic Cafe, 2013) is the closest relative: a narrator comments on the player's choices and treats disobedience as something the author planned for. We differ in three ways. The player is embodied in VR, the narrator never scolds and instead reframes, and accumulated behaviour changes the room and the narrator's composure, not only which branch is shown. Half-Life: Alyx (Valve, 2020) differs most. In the lecture's terms it combines scripted plot, high sensory detail and strong global intelligibility, with limited emergent content; interaction mainly serves physical engagement. Our piece has low fidelity and almost no challenge, and the interaction is the narrative choice. Milgram (1963) is a source, not an IDN: we borrow the authority frame and polite instructions but replace shocks with a harmless cube and button.

*Word count: 268 / 300*

### Strongest element and what we would change

The strongest element is the refusal ending. Doing nothing is the cheapest input to build and the hardest for the narrator to explain, so a small rule set produces a moment that feels authored: the voice moves from third person to first, the room dims, and the "Participation is voluntary" line from the opening returns with a different meaning.

With more time we would run proper playtests to learn how many players try doing nothing, and what they think they were being measured on. We would improve inactivity detection with eye tracking, add a fourth test so that early refusals could change later tests more, and record several voice takes so repeated plays feel less fixed. We would also test whether the AS EXPECTED card is understood without a facilitator.

*Word count: 130 / 150*

## 7. References

Bruni, L. E., & Baceviciute, S. (2013). Narrative intelligibility and closure in interactive systems. In H. Koenitz et al. (Eds.), Interactive Storytelling: ICIDS 2013 (LNCS 8230, pp. 13-24). Springer. [EDIT: verify pages]

Bruni, L. E., Kadastik, N., Pedersen, T. A., & Dini, H. (2022). Digital narratives in extended realities. [EDIT: add publication details from the Moodle reading list]

Genette, G. (1980). Narrative discourse: An essay in method. Cornell University Press.

Milgram, S. (1963). Behavioral study of obedience. Journal of Abnormal and Social Psychology, 67(4), 371-378.

Nilsson, N. C., Nordahl, R., & Serafin, S. (2016). Immersion revisited: A review of existing definitions of immersion and their relation to different theories of presence. Human Technology, 12(2), 108-134.

Ryan, M.-L. (2001). Narrative as virtual reality: Immersion and interactivity in literature and electronic media. Johns Hopkins University Press.

Ryan, M.-L. (2015). Narrative as virtual reality 2: Revisiting immersion and interactivity in literature and electronic media. Johns Hopkins University Press.

Slater, M. (2009), cited as in Nilsson et al. (2016), for place illusion and plausibility illusion. [EDIT: secondary citation, no separate entry unless you read the original.]

Ludography

Galactic Cafe. (2013). The Stanley Parable [Video game]. Galactic Cafe.

Valve. (2020). Half-Life: Alyx [Video game]. Valve.
