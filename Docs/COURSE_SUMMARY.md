# Narratives in Interactive Systems (CPH, MED7): study summary

5 ECTS. Oral exam based on a mini-project (7-point scale, individual grade). Lecturers: Luis Emilio Bruni, Niels Erik Raursoe.

Sources read in full or in part:

- Ryan, *Narrative as Virtual Reality* (2001, "NVR1"): Introduction, chapters 3, 6, 8, 10 in full.
- Ryan, *Narrative as Virtual Reality 2* (2015, "NVR2"): scanned copy, not used directly. Chapter numbers differ (see section 8).
- *Cambridge Companion to Narrative* (2007): Ryan ch. 2 and Montfort ch. 12 in full.
- Lecture slides 1.1 to 3.2 (Days 1 to 3), 5.1 and 5.2 (Day 5), mini-project guidelines and DRIS template. Day 4 and Day 6 decks are still missing from the folder.

Not read: Ryan ch. 1, 2, 4, 5, 7, 9 (only from Introduction and lecture slides), the other Companion chapters, and the supplementary papers (Nilsson et al. 2016, Bruni and Baceviciute 2013, Murray 2018, etc.).

---

## 1. Course frame

Mandatory reading and exam pensum come from the Moodle syllabus:

| Day | Date | Topic | Mandatory reading |
|---|---|---|---|
| 1 | 04/09 | Narrative and narratology | Ryan (Companion ch. 2), Murray 2018 |
| 2 | 11/09 | Interactivity, agency, narrative paradox | Ryan NVR2 ch. 7 (or NVR1 ch. 8) |
| 3 | 25/09 | Immersion, presence, engagement | Nilsson et al. 2016, Ryan NVR1 ch. 4 and 5 |
| 4 | 02/10 | XR: embodiment, focalization, agency | Bruni et al. 2022 (XR chapter), Ryan ch. 2 |
| 5 | 14/10 | AI storytelling, control, intelligibility | Bruni and Baceviciute 2013 |
| 6 | 23/10 | Transmedia, rhetoric, ethics, presentations | Ryan 2015 transmedia, Green and Jenkins 2014, Bruni 2010 |

Mini-project: 3 ECTS (about 90 h), prototype of an interactive narrative in a chosen domain, group work but individual exam. Written statement due 14 December per earlier notes, but the Moodle guidelines only say "deadline announced on Moodle" (no date in the documents, check Moodle). Work-in-progress showcase on Day 6 (5 min per group). Workshops on each day feed a portfolio for the project. Exam: individual oral on the project plus questions from the pensum.

## 2. Day 1: Narrative and narratology (04 Sep, lectures 1.1 to 1.3)

**Framing and domains** (1.1, 1.2)
- Course targets designing, implementing and evaluating Interactive Digital Narratives (IDN). Content implies narrative, narrative implies rhetoric and aesthetic choices, and narratives become interactive.
- Domains shown, with the design point of each:
  - **Edutainment:** Lifesaver (CPR game, 2M+ users), AVEnueS (VR training for caseworkers), Micro Kingdoms (AR spatial storytelling).
  - **Health and fitness:** Zombies, Run! (audio narrative drives running). Supernatural (VR fitness with no narrative, lecture asks if story would help).
  - **Journalism and i-docs:** NYT Displaced (1M Cardboard headsets), Do Not Track (personalized by your data), Bear 71, MIT Docubase.
  - **Serious games:** Molleindustria. **Advertising:** Honda The Other Side. **Culture:** CHESS (museum authoring tool), Curious Alice.
  - **Entertainment:** Bandersnatch (Netflix built a tool because of combinatorial explosion), The Quetzal (EEG, early adaptive narrative), Before Your Eyes (eye tracking).
- Emerging paradigms named for later: AI storytelling, intelligent characters, XR, transmedia, environmental storytelling, IDN for complexity and big data, adaptive narratives, storyworld construction, location-based IDN.
- Mini-project: 3 ECTS, about 90 h, group work allowed, individual exam. Oral exam covers the project plus the pensum. Written statement due 14 December.

**Narratology as design toolkit** (1.3)
- Choosing a branching structure is a discourse decision. Deciding what users can change is negotiating agency. The design rationale needs this vocabulary ("understanding narrative structure is to the IDN designer what harmony is to a composer").
- **Story vs discourse**: what is represented vs how. Histoire/discours (French), fabula/syuzhet (Russian formalists). Story is transposable across media (Chatman), which is why narratology applies to IDN.
- **Three layers**: fabula (causal network of all events), plot (a coherent selection, many plots per fabula), syuzhet/discourse (presentation in a medium).
- **Structuralists share one ambition**: find the deep grammar of narrative. If it has a grammar, it can be formalized and computed (link to Day 5).
  - **Propp**: 31 functions, 7 character types. Maps to donor items and upgrades, quest departure and return, villain as conflict driver.
  - **Genette**: order (flashbacks, unlockable backstory), duration (real-time play vs scripted), frequency (repeated NPC lines, revisit from other POV), mood and voice (who sees vs who speaks, avatar vs narrator).
  - **Barthes**: functions, actions, narration. **Chatman**: events (actions, happenings) and existents (characters, settings).
- **Diegesis**: the story world's time-space continuum. Suspension of disbelief means accepting it. Plot is etymologically a "ground plan", structure is how the work orders it.
- **Ryan's scalar narrativity** (Companion ch. 2): 8 conditions in semantic, formal and pragmatic dimensions.
  - Genres weight one dimension: spatial (SF, fantasy), temporal (thrillers), mental (tragedy, romance, detective).
  - Lord of the Rings scores high everywhere. A Minecraft sandbox has rich space but weak time, mind and closure: low, not zero.
- **Incremental definition**: sequential ("king died, queen died"), causal ("died of grief"), dramatic (equilibrium, crisis, new equilibrium, Freytag triangle, conflict, rising action, climax, resolution).
  - In an interactive setting, sequential narrative is produced by play time itself. Causal is adventure games, wrong turns do not break coherence. Dramatic needs the system to steer choices without revealing it, so it is the hardest.
- **Hero's Journey** (Campbell): departure, initiation, return. Widely used in games (Zelda, Mass Effect). Open question: what if the user refuses the call? That leads to the narrative paradox.
- Workshop 1: story and discourse analysis of a chosen work, structural analysis, dramatic arc, first brainstorm.

## 3. Day 2: Interactivity, agency, the narrative paradox (11 Sep, lectures 2.1 to 2.3)

**IDN history** (2.1)
- Memex 1945, hypertext 1965, Colossal Cave 1976, Infocom, Storyspace and HyperCard 1987, Myst 1993 (environmental storytelling), Facade 2005 (AI drama), Twine and ChoiceScript 2009, Walking Dead and Bandersnatch (mass audiences), LLMs from 2019.
- Four foundational forms and their legacy:
  - Text adventures (Zork): parser, world model, state tracking. Today: voice assistants, chatbots.
  - Hypertext fiction (afternoon): non-linear reading, link navigation. Today: Wikipedia, Twine.
  - Visual novels (Chunsoft sound novels): route and ending structures, relationship sims.
  - Graphic adventures (Myst): exploration, walking simulators, escape rooms, VR.
- Ludology vs narratology debate is now read as co-constitutive ("ludologists love stories, too", Frasca).
- Case study: Realidad Helicoide (Voces de la Memoria), an immersive testimony piece in Unity for Quest 2, 15 to 30 min and a 5 to 7 min version, Spanish and English narration.

**Agency and the narrative paradox** (2.2)
- **Agency**: ability to make choices and see and enjoy the results. Not just action verbs: navigation, avatar creation, POV change, building, adding text.
- **Run-amuck agency**: too much power lets users delete the antagonist and solve everything, erasing drama.
- **Narrative paradox** (Louchart and Aylett 2003): the more freedom a player has, the harder it is to convey a pre-conceived narrative.
- **Lecturer's warning**: do not reify it. It is context dependent, in some cases it ceases to be a paradox or is even desired (emergent narratives).
- **Method**: identify the specific conflict in your project, design targeted solutions (adaptive storytelling, branching, emergent), measure coherence against agency, iterate.

**Frameworks for interactivity** (2.3)
- A narrative implies immersion but not interaction. Interactivity is either inherent to the medium or inherent to the work. All interactive works need an interactive medium, the reverse does not hold.
- **Ryan 2000, freedom and intentionality**: reactive, selective (random selection or purposeful), productive. Games sit halfway.
- **Ryan 2015, two binary pairs**: internal vs external and exploratory vs ontological.

| | Exploratory | Ontological |
|---|---|---|
| External | classical hypertext | Sims, Civilization |
| Internal | embodied POV, no power (environmental storytelling?) | FPS, adventure, tabletop RPG, Second Life |

- **Ergodic design** (Aarseth): built-in feedback loop so the text modifies itself and shows different sequences per session.
- **Jenkins 2002**: evocative spaces, enacting stories (Journey, Passage), embedded narratives (L.A. Noire, Gone Home), emergent narratives (Skyrim).
- **Emergent narrative = rules + simulation + interpretation**. The system generates events, the audience links them (Sims, RimWorld, Dwarf Fortress).
- **Can coherence be saved?** Classic narrative is still the best immersion recipe, hence scepticism. Reconciliation means integrating bottom-up user input with top-down author design.
- **Link structures** (Ryan ch. 8): the potential to generate well-formed stories per traversal is inversely proportional to connectivity.
  - Complete graph: practically impossible to keep coherent.
  - Network: continuity only local, standard for literary hypertext.
  - Tree: controlled but combinatorial explosion, Choose Your Own Adventure.
  - Vector with side branches: linear plot, side trips.
  - Maze: coherence from shared goal, aporia and epiphany.
  - Directed network / flow chart: best fit for drama, but mid-story choices are trivial without memory of choices.
  - Hidden story: fixed story plus investigation network.
  - Braided plot: switch character perspectives.
  - Action space: macro freedom, scripted micro-episodes, one of the other models inside each site.
- Workshops: apply Ryan's frameworks to your idea, and to the interactive film Late Shift.

## 4. Day 3: Immersion, presence, engagement (25 Sep, lectures 3.1, 3.2)

**Theories of immersion** (3.1)
- **Jungle of terms**: engagement, presence, concentration, believability, transportation, realism, suspension of disbelief, transparency of the medium, suspense, attention, emotional response, flow, imaginative involvement, involvement, contemplation, plausibility, absorption, agency, embodiment. Fields talk past each other, so the lecture offers a map, not a new definition.
- **Three layers**:
  1. **Technological**: what the system delivers (FOV, tracking, latency, resolution, haptics, spatial audio). Objective, system side, sets the ceiling. The layer designers control most directly.
  2. **Perceptual/cognitive (phenomenological)**: presence. Perceptual, vestibular, proprioceptive and autonomic systems act as in real life while you know it is not real. Mismatches break it and cause cybersickness. Measured through the user (scales, behaviour, physiology).
  3. **Semiotic/narrative (mimetic)**: the world feels autonomous and inhabited. Needs internal logic, populated characters and cause-effect, not photorealism. A novel can do this with no technology.
- **Slater 1999**: immersion is what the technology delivers, presence is the human response. Analogy: wavelength distribution vs perceived colour (metamers). Same system can give different presence to different people.
  - Lecturer's critique: this is an error of logical typing, it confuses the immersive system with immersion, and judging a system's immersiveness is still subjective.
- **Form vs content** (Slater): a flawless system can fail if the content bores.
- **Evolution of technological immersion**: panoramas, cinema, IMAX, surround sound, first-person games, VR, AR, MR, haptics.
- **Hierarchy for diagnosis**: 0 technological, 1 sensual-perceptual, 2 emotional, 3 semiotic-intelligible. Objective at the sensory end, subjective at the semiotic end, never fully objective. When an experience fails, ask at which level it breaks.
- Workshop: sort the jungle, diagnose a failure, map your own experience.

**Narrative immersion** (3.2): three forms of involvement, matching setting, plot and character.
- **Spatial**: sense of place (atmosphere) plus a mental model of place (map). Readers picture the changing landscape along characters' routes.
  - Literature turns the mind into a theatre. Degree of detail depends on the reader (visualizer vs plot reader) and on focus (character, plot, setting).
  - **Madeleine effect**: one word, name or object transports you into a landscape, depending on resonance with personal memory.
  - In contrast to VR, quality depends on salience of features, not intensity of information. Too much detail loses the subject in data.
  - Display spatiality (surround, depth, roving POV) is separate from narrativity of the space.
- **Emotional**: Proust's grief for characters. Since Aristotle: catharsis, pity and terror.
  - Characters have a hybrid mimetic dimension, human enough to justify emotion, pseudo enough to be problematic. Responses cross the **ontological boundary**, needing suspension of disbelief.
  - Emotions in fiction differ from real ones: sadness or terror becomes pleasure, physical symptoms do not stop enjoyment, crying for a character does not make you sad, fear does not make you leave the theatre.
  - **Currie**: emotions link feelings, beliefs and desires. In fiction you have make-beliefs and make-desires, so quasi-emotions.
- **Temporal**: the desire for the knowledge that awaits at the end of narrative time. Not the same as temporal dissociation.
  - **Suspense**: partial, anxious uncertainty about how an action will end. Needs a structured horizon of anticipation (Carroll), built from knowledge of characters, goals and plans. Hitchcock's device is telling the audience the facts.
  - Intensity is inversely proportional to the range of possibilities. Everything is open at the start, a problem narrows options, a choice polarizes into success or failure, which starts the climax.
  - **Laurel's Dramatic Flying Wedge**: wide base is the start, narrow tip the end. Possibilities shrink and intensity grows, and user choices pick the path.
  - **Suspense vs surprise**: no consensus. Surprise is about unmet expectations in the here and now, suspense projects to the future. Surprise may only enter at resolution, or suspense may be built from local surprises. A resolution nobody foresaw can be more satisfying.
  - **Four kinds**: "what" (Joyce's Eveline, needs emotional involvement), "how/why" (multiple paths converge, curiosity), "who" (mystery, intellectual, structured field limits suspects), meta-suspense (critical interest in how the author will close the strands).
- Workshops: spatial, emotional and temporal immersion applied to your mini-project.

## 4b. Day 5: AI storytelling, intelligibility and closure (14 Oct, decks 5.1, 5.2)

**From computational narratives to AI storytelling** (5.1)
- **Hook**: from the 1980s text adventure line "You are in a dark room... escape or stay?" to an LLM NPC saying "Hi, I am Hamlet; you know me from Shakespeare...".
- **Lineage**: Digital Narratives split into Computational Narratives (precursor to AI storytelling) and Interactive Digital Narratives, IDN (precursor to XR storytelling). Recurring question: "What happened to narrative control?"
- **IDN vs computational narrative** (ICIDS and beyond):

| | Interactive Digital Narrative | Computational Narrative |
|---|---|---|
| Focus | User agency, experience, choice. Story emerges from system plus user. Central tension: the narrative paradox | Automated story generation and narrative intelligence, often without user input. Challenge: coherent, interesting narratives automatically |
| Role | Co-author, player. Control shared | Reader, observer or absent. Control system-driven |
| Research questions | Agency, coherence despite choices, engagement. Branching, choice architectures, adaptive and emerging narratives | How to generate good stories, structure, character believability and goal-driven behaviour, ontologies, planners and drama managers |
| Evaluation | UX and engagement, sense of agency, replayability, meaningful choice, emotional impact | Story quality (coherence, interest, creativity), well-formedness, believability, generative variety |

- **Overlap**: both import narratology (structural), need character models, world representations and causality. Interactive narratives use planners and drama managers, computational ones can take user input. "More about emphasis than strict boundaries": interactive asks how we tell stories *with* users, computational how we tell them *algorithmically*.
- **Structuralist narratology**: narratives are instances of underlying structures, functions and codes analysable across texts ("from Propp to Chatman").
- **Classic components of a computational narrative system** (7 core):
  1. **Ontology / knowledge representation (world model)**: entities, locations, characters, properties, spatial/social/causal relations, domain rules.
  2. **Drama manager (story director, story level)**: pacing, tension, arc, coherence, balances player agency with authorial intent, manages plot points, beats and goals.
  3. **Character system (agent model, can sit in the ontology)**: goals, motivations, personality, beliefs, social relations, emotions, decision-making.
  4. **Planner (action selection, character level)**: goal-driven planning, plan libraries or generative planning, preconditions and effects, may include social planning (persuasion, deception, cooperation).
  5. **Discourse / presentation layer (realizer)**: turns internal story into text, dialogue or visuals, NLG or templates, perspective and focalization, pacing and information revelation.
  6. **Inference engine (causality and consequences)**: derives implicit consequences, updates world and character state, causal chains, may trigger new goals or events.
  7. **Story memory (event history)**: coherence, callbacks, pronoun resolution. May distinguish fabula (story events) from sjuzhet (discourse).
  - **Optional (3)**: authoring tools (visual editors, scripting, constraints), user model (tracks choices and engagement, adapts), evaluation/reflection module (assesses coherence, interest, dramatic value, can guide the drama manager).
- **Evolution**: computational rules, then intelligent systems, then AI creativity. Early approaches: story generation systems, interactive fiction engines, rule-based frameworks. Guiding questions: what problems did pioneers try to solve, why tell stories with computers, what were the limits?
- **Pioneers and cases**:
  - **Eliza (1966/67, Weizenbaum)**: Murray ("Kaleidoscopic View"): drew on narrative tropes, the neurotic seeking help and the absurdly "non-directive" therapist. "The interactive equivalent of sketch comedy." It created the illusion of a real person, so the creator warned against automated therapists. Now seen as a weak version of the chatbot genre.
  - **TALE-SPIN (Meehan, 1976/77, "Inside Computer Understanding" 1981)**: writes simple stories, unlike filling slots in a canned frame. Goal: find out what knowledge story generation needs. Followed the traditional AI cycle: define a theory, program it, run it, see where the model is inadequate, which exposes the need for more theory.
  - **Façade (2005)**: first intelligent agents plus drama manager. **Zork I** (Infocom interactive fiction) and **Character.AI** shown as demos. (Dates for Eliza and TALE-SPIN vary by one year inside the deck.)
- **Three building blocks: agents, managers, generators** (ontology becomes the "story world"):
  - **Intelligent agents**: scripted, then FSM / Behavior Trees, rule-based, then goal/utility-based (GOAP, utility curves), then learning-augmented (analytics-tuned, occasional ML). Intelligence in IDN is with respect to the plot and with respect to the user.
  - **Drama managers**: "the invisible director" / game master. Schedule beats (setup, escalation, climax), balance difficulty and pacing, keep coherence and authorial intent across branches, adjust intensity, timing of reveals and consequences, pick the next scene under constraints (no early spoilers, arc intact).
  - **Procedural content generation (PCG)**: random, then rule/grammar/template-based, then search/constraint satisfaction for validity, then AI-assisted (LLM, diffusion) with human-in-the-loop control ("prompting engineering?"). Generates quest structures, clue chains, branching prompts, dialogue variants, item and character descriptions, environmental storytelling.
- **Opportunities**: "infinite" content, personalised/adaptive narratives, more natural character interaction, lower barriers to story creation. **Challenges**: coherence, quality and depth; authorial control; technical limits; ethics (bias, misinformation).
- **Conclusion**: AI pipelines, procedural assets, LLMs, neural generation and AI NPCs with contextual dialogue are transforming traditional paradigms (real-time personalised experiences).
- Workshop 5.1: computational narrative building blocks (title only on slide).

**Intelligibility and closure** (5.2, Bruni)
- **Narrative games vs playable stories (Ryan 2009)**:
  - *Narrative games*: gameplay and systemic play first, narrative meaning subordinated to player actions and world simulation, player plays to beat the game. Rich agency, but narrativity is emergent, interpretive or optional, "mostly a lure into the game world".
  - *Playable stories*: authored arc first, interactivity constrained to protect intelligibility, pacing and closure, no winning or losing, the player observes the evolution of the storyworld. Coherence kept, but agency can feel cosmetic, guided or illusory.
- **When did stories stop behaving?** Linear narratives (oral epic, novel, film): the author is architect and sole gatekeeper of meaning. "Lineal is not sequential": the author controls the order of the discourse.
- **Control is not intelligibility**: authorial control does not guarantee clarity (abstract, ambiguous, poorly crafted stories). The gap between intention and interpretation has always existed: **Author-Audience Distance (AAD)**. Abstraction or ambiguity can be intentional.
- **Cooperative principle (Grice; Young 2002)**: effective interaction rests on shared expectations. Four maxims: **Quantity** (enough, not too much), **Quality** (truthful, evidenced), **Relation** (relevant), **Manner** (clear, orderly). In non-interactive narrative the author's intentions dominate, in interactive systems the audience's intents add complexity. The principle now runs between user and system, with the author's intent still significant. Interactive scenario = system boundaries, degrees of freedom granted, rules for using that freedom, cueing of further actions (feedback, affordances).
- **AAD as a function of narrative intelligibility (Bruni and Baceviciute 2013)**, two poles for the story or its representation:
  - **Abstract**: content with only intrinsic form, little or no pictorial, figurative or explicit representation, but potential to elicit some narrativity. High abstractness gives confused, incomplete interpretations and referential ambiguities.
  - **Didascalic** (Greek/Latin *didascalia*): didactic, explicit, obvious, self-explanatory, high pictorial, descriptive, figurative power. Creates the illusion that little interpretation is needed and "the story is as it is".
  - Anchors: Eco (1981) "aberrant decoding" to perfect reception of the preferred decoding; Bruner (1991) "the story as it is"; shared coding/decoding system between author and audience.
  - Workshop 5.2 [1.1-1.4]: "A visual story" (title only). Workshop 5.2 [1.5-1.6]: "Author-audience distance" (title only).
- **Interactivity precedes digital media**: oral storytelling, Greek Chorus, Commedia dell'arte, branching or participatory print, RPGs.
- **Ryan (2001) types of interactivity** (increasing agency): 1 reactive (no deliberate action), 2 selective (2.1 random selection: deliberate act, unforeseeable result; 2.2 purposeful: choice between paths), 3 productive ("the fullest": leaves a durable mark on the virtual world by adding objects or influencing the narrative). The mini-project guideline adds exploratory and ontological.
- **Narrative control**: do we want it, and why (coherence, author intention, goal, intelligibility, closure)? When, why and with what aim do we give it up?
- **Can coherence be saved?** Ryan (2001): immersion vs interactivity trade-off, classic narrative is still the best recipe for immersion, hence scepticism about interactive media.
- **Emergent narratives as solution to the paradox?** The story emerges from gameplay, not pre-scripted. The designer fills the space with "narrative potential", the audience interprets what emerges. Risk: **autotelic** systems with no explicit goal producing narrative for its own sake ("emancipation from the tyranny of the author"). Example: Samorost 2 (Amanita Design).
- **Three narrative-goal cases in IDNs (Bruni and Baceviciute 2013)**:
  1. The goal of the system supersedes the narrative. The narrative is a tool, authorship resides in the goal. Faithful reception may not be the system's goal.
  2. Authorship resides in the narrative, the system is just a medium. Quality and trustworthy transmission are the central element and coincide with the goal.
  3. Both goal and narrative disregarded (total user freedom). By reductio ad absurdum, we only give the audience an expressive tool to become authors (sandbox games).
- **Narrative intelligibility vs closure**: two interrelated meaning-generation processes.
  - **Intelligibility**: the audience understands the substance close to the author's intended meaning (high-fidelity transmission).
  - **Closure**: the audience feels coherence and completeness afterwards, even if what they understood is not what the author intended.
  - **Asymmetric relation**: "intelligibility presupposes closure, but closure may be reached without an intelligible intended message." Cases: intelligibility with closure; closure without intelligibility; intelligibility without closure (open "???"). Example image: The Witness (Thekla).
  - AAD is a function of intelligibility (itself shaped by abstractness and didascalicity). Large AAD can coexist with constructed meaning (emerging narratives).
- **Two levels of analysis**: the designer defines intelligibility and closure at the **system level** and the **narrative level**.
  - *Dear Esther*: low intelligibility, yet high closure is possible (artistic, literary, emergent systems).
  - If the system's goal needs open interpretation (large AAD, abstract narrative), the user can reach overall intelligibility or closure by attaining the system's goals (edutainment, persuasion, therapy, games).
  - Some FPS games: narrative in cinematic cuts not central to system goals.
- Workshop 5.3: towards the mini-project (title only).

**XR and the intensification of presence** (5.2 slides 36 to 40, Day 4-type material)
- XR as a narrative medium. As in IDN, trade-offs between interactivity and immersion. Needs particular care: agency, embodiment, narration/focalization ("point of view technologies"), artificial agents and AI.
- **The narrative paradox is harder to balance in XR**: embodiment plus POV technologies intensify participation and expectations of influence. Avoid "run-amuck agency" that works against dramatic arc. Body, hands, gaze and locomotion push toward narrative games. Tune interaction affordances and closure/intelligibility to your goals. XR's high didascalicity (multisensory explicitness) shortens AAD but raises the stakes of intelligibility failures.
- **Embodiment and focalization**: embodiment and disembodiment bear on narration, focalization and perspective. The user is a situated agent, not a spectator, pushing toward **first-person internal focalization**. Who narrates? How do you put thoughts into the player? Technique: "surfacing imperceptibles" via companion voice-over, inner monologue, diegetic journals.

**AI storytelling and algorithmic anarchy** (5.2 slides 41 to 54)
- **From drama managers to generators**: drama managers follow strict logic (expert-system like), LLMs "improvise". The "let it write itself" trap gives **Algorithmic Anarchy**. Minimal control leads to narrative collapse: fluent prose lacking long-term pacing, emotional depth, logical consistency.
- **Three layers of generative AI**:
  - *Sensory* (form, immersive flavour): images, 3D, textures, foley, spatial audio, animation. Raises vividness and sensory expectations. Example: Half-Life: Alyx (scripted plot, high sensory detail), strong global intelligibility, limited emergent content.
  - *Narrative content* ("story meat"): dialogue, backstory, descriptions, event ideas (LLMs). Gives local didascalicity. Example: AI Dungeon, high local novelty, weak plot necessity, episodic.
  - *Systemic/drama control* (discourse, "who arranges the meat"): branching, pacing, sequencing, constraints, drama managers, planners, hybrid drama manager plus LLM. Preserves or disrupts global intelligibility. Example: Façade.
- **New triadic paradox**: the system becomes a narrative actor, so the paradox is author, audience, agent (LLM), not just author and audience.
  - Author and User: interpretation vs control (AAD problem). User and Model: agency vs story structure (traditional paradox). Author and Model: authorial intention vs algorithmic anarchy (new layer).
- **AAD revisited**, multiple distances: Author to User (intended vs received), User to Model (does the user understand what kind of communicator the system is?), Author to Model behaviour (does the generator preserve intent?).
- **Three collapses**:
  1. **Meaning (intelligibility)**: AI drifts, AAD gets too wide, the system still runs but no longer says anything.
  2. **Structure (closure)**, the Aristotelian collapse: vivid fragments without causal connection, the user stops attempting closure, the story becomes disconnected tech-demos or chatbot responses.
  3. **Cooperative contract**: hallucination or "statistical plausibility" violates world logic, choices lose predictable consequences, agency becomes meaningless.
- **Scale paradox, layered approach**: author keeps **global control** (themes, major arcs, world logic), AI handles **local generation** (moment-to-moment dialogue, sensory description, emotional texture). **Control points**: predefined nodes where constraints relax for user detours, then guide back to the global arc. "Orchestrating the orchestra": the future is better systemic design, not better LLMs; embed AI in narrative logic rather than letting it be the sole storyteller.
- **Aristotelian warning**: "A string of vivid, well-developed fragments is not a story if the plot is weak."
  - *Episodes* = local didascalicity: polished speeches and characters fail to give the "proper pleasure" of tragedy without a necessary plot (LLM: vivid moments, no global arc).
  - *Plot (muthos)* = global intelligibility: every event follows from the last, so the "why" is clear even if local parts are simple.
  - Generative AI is "the ultimate Episodic Poet": local didascalicity, global abstraction, the user must perform closure to invent a missing "why", and emotional impact is lost.
- **Future**: AI as "flavour", not the whole story. AI for narrative orchestration and believable agents, not replacing the author. Ethical and aesthetic responsibility: a new design grammar that embraces generative unpredictability without losing human meaning-making. All depends on the goals of the system and of the narrative.
- Workshop 5.4: goals of the system and of the narrative (title only).

## 4c. Mini-project guidelines and DRIS template (Moodle documents)

**Mini-project guidelines (NIS 2026, MED7)**
- **Task**: groups conceive, design and build a concrete interactive narrative prototype applying course concepts. Workshops progressively advance it. Workload 3 ECTS = 90 h per student.
- **Theme and domain** (open): remediation of a literary or historical work; narrative game or gamified experience (choice-based, puzzle-driven); social, cultural, political or environmental issue (immigration, cultural heritage, health, ecology, terrorism, mobbing, populism, inclusion/exclusion, identity); educational, persuasive or documentary narrative. Domains: serious games, interactive drama, edutainment, advertisement, gamification, interactive documentary, museum experience. A separate list of platforms and tools exists.
- **Part 1, Design**: describe key narrative aspects, characteristics, intended audience and especially purpose and goals. Considerations:
  - **Narrative structure**: branching, adaptive, procedural; interactivity type (selective, productive, exploratory, ontological).
  - **Agency**: freedom and intentionality vs coherence.
  - **Immersion**: spatial, temporal, emotional.
  - **Embodiment and focalization**: user's role, who speaks, who sees, from where.
  - **AI / computational elements** (if applicable): how AI, LLMs or procedural generation are used and how narrative control is kept.
  - **Narrative intelligibility and AAD**: intended AAD, open interpretation vs precise message.
  - **Rhetorical and aesthetic choices**.
- **Part 2, Implementation**: working prototype on any platform. Need not be polished but must show the core narrative and interactive logic and be demonstrable at the oral exam.
- **DRIS (Design Rationale and Implementation Statement)**: one per group, not a scientific paper, no user testing or evaluation required. Documents and justifies decisions with course concepts and IDN literature. Must follow the Moodle template, uploaded as PDF. Previous workshop outputs can be integrated.
- **Submission and exam**: DRIS and prototype (or in some cases a walkthrough video) to Digital Exam before noon on the deadline (announced on Moodle). Max 5-minute work-in-progress presentation per group on the last day of class. Final exam is individual: oral discussion of the project plus curriculum questions, one group demo is enough, then each member is assessed separately. 7-point scale, based on the ability to discuss and apply course concepts to the project. Groups formed by agreement with the teacher.

**DRIS template sections and word limits** (firm guidelines, small overruns tolerated, grey boxes for visuals, erase the instructions, design considerations are suggestions)
1. **Project Overview**: max 150 words (domain and context, audience and engagement, central premise).
2. **Narrative Design and Interaction**: max 350 words plus a structure diagram or branching map (structure and interactivity type, agency, embodiment and focalization, rhetorical and aesthetic choices).
3. **Immersion Aspects**: max 250 words plus an environment sketch or screenshot (spatial, temporal, emotional immersion; intelligibility and AAD; aesthetics vs immersive engagement).
4. **AI and Computational Elements**:
   - 4a. AI in the implementation (only if applicable), max 200 words: role of AI, narrative control, AI avatars/agents/NPCs and how they are authored or constrained, authored vs generated balance, effects on agency, immersion, intelligibility.
   - 4b. AI in the design and production process, **mandatory for all groups**, max 200 words: ideation, asset generation, writing help, critical reflection. If no AI was used, say so explicitly.
5. **Implementation**: max 200 words plus a system/architecture diagram and 2 to 3 annotated screenshots (platform affordances, fidelity and scope, limitations).
6. **Design Rationale Synthesis**: max 300 words. Pick 2 to 3 key concepts and show how they shaped decisions, with a comparison to at least one existing IDN the project resembles and one it differs from. Plus max 150 words: strongest element and what you would change.
7. **References**. Cover page: title, group number and date, members.

## 5. Ryan chapters read in full

### Ch. 3: The Text as World, theories of immersion
- Immersion needs a **textual world**: four features, a connected set of objects and individuals, habitable environment, intelligible totality, field of activity. Language is traversed toward referents ("vertical" meaning). Applies to **mimetic texts** (individual existents in time and space), fiction or nonfiction.
- **Transportation** (Gerrig): traveler, vehicle, performing actions, going away, world of origin inaccessible, return changed. Reading is performance. Minimal transportation is built into reference ("Texas"), strong form needs aesthetic features.
- **Lost in a book** (Nell): effortless attention from automated decoding, so familiar text is most immersive. Difficult text can pay off after work. Immersion must be temporary and split-minded ("dreaming when you know you are dreaming"), addiction is its harmful cousin. Four degrees: concentration, imaginative involvement, entrancement, addiction (incl. Don Quixote syndrome).
- **Possible worlds**: recenterable model. Counterfactuals are **telescopes** (view another world from outside), fiction is a **space-travel vehicle** (**recentering**, actuality is indexical). Texts project a modal system around a textual actual world. Nonfiction has a construction moment (imagine) and an evaluation moment (assess accuracy).
- **Make-believe** (Walton): representation is a prop in a game of make-believe, fictional truths are true in the game. Ryan splits make-believe into (1) pretending truth (fiction vs nonfiction) and (2) mental picturing (a matter of degree, drives immersion).
- **Mental simulation**: simulate from inside the characters, living time prospectively. Explains emotional involvement and why authors say characters take over. Interlude: Ignatius of Loyola's Spiritual Exercises (all senses, body and soul).

### Ch. 6: From immersion to interactivity, text as world vs text as game
- Poststructuralism hostile to transparency of signs. Ryan argues transparency is a hard-won property, and proposes the **text as game** as complement.
- Game concept is a family resemblance (Wittgenstein), core features: pleasure, rules (ludus vs paidia). Literary uses: literal (riddles, word games, text adventures), metonymic (shuffled cards, I Ching, clicking links), narrow metaphorical (Perec, Calvino's tarot), broad metaphor (weak).
- Rules differ: games have constitutive rules, followed strictly, learned first. Literature has regulative conventions, transgressed, learned while reading.
- **Caillois**: agon (competition, hypertext as labyrinth or puzzle), alea (chance, Oulipo, Queneau, Cayley), mimicry (make-believe, already covered so excluded), ilinx (vertigo, free play, carnivalesque, postmodernism). Shift from ludus to paidia with Derrida (free play, decentered) and Foucault.
- **Comparison table** (game vs world): language function (construction toy vs mirror), substance (opaque vs transparent), meaning (horizontal, emergent vs vertical, referential), reader attitude (critical distance vs suspension of disbelief), activity (build/deconstruct vs explore as tourist/voyeur), form (exoskeleton vs internal skeleton), chance (ambiguous vs negative), space (2D playing field vs 3D environment), requirements (elite competence vs populist), Barthes writerly vs readerly.
- Turkle analogy: MS-DOS (world, transparent as revealing) vs Mac (game, surface, transparent as invisible).
- Conclusion: game and world are **complementary**, like wave and particle. You cannot occupy both viewpoints at once, so you must immerse and deimmerse periodically.

### Ch. 8: Can coherence be saved? Selective interactivity and narrativity
- Two views of narrativity: culturally variable form (hypertext theorists) vs universal cognitive model with degrees (Ryan's view). Commercial designers take the Aristotelian view.
- Sequential, causal, dramatic narrative, each harder to realize interactively. Dramatic needs steering the user without revealing it.
- **Structures** (figures 4 to 12), with tradeoffs:
  - Complete graph: no coherence guarantee.
  - Network: coherence only local, suited to analogy and surrealism.
  - Tree: controlled but combinatorial explosion (4 decision points need 16 plots, 31 fragments), Choose Your Own Adventure.
  - Vector with side branches: linear story plus side trips (Arthur's Teacher Trouble), guided tour (The Spot).
  - Maze: adventure games, aporia and epiphany (Aarseth), coherence through shared goal.
  - Directed network / flow chart: best reconciliation with drama, but mid-story choices are trivial without **narrative memory** (context-sensitive, Proppian donor example).
  - Hidden story: two levels, fixed unilinear story plus investigative network (Myst).
  - Braided plot: switch among character perspectives (Lurker Files).
  - Action space / epic wandering / story-world: macro freedom, micro scripted adventures (theme parks, Hypercafe).
- **Rule**: potential to generate well-formed stories per traversal is **inversely proportional to connectivity**.
- Fixes: control path, limit decision points, neutralize consequences. Or use **microevents** (click triggers small delight, Hughes' jagged Freytag triangle). Interactive curiosity should be local, print is better for long-term "how does it end".
- **Hypertext and immersion**: temporal (broken structure hurts memory, 64 endings are noise, 2 to 4 work), spatial (hyperspace, non-places, smooth space, lexias too short to settle, Calvino's interrupted openings, hypermedia can compensate), emotional (Murray: comic vision, retrievable mistakes, Eco: hypertext cannot satisfy because destiny is missing). Multiple worlds are viewed from outside, so recentering fails.
- **Future**: (1) drop the novel model, use short lexias and multimedia (Things Spoken), (2) merge with games, (3) conceptual art (Agrippa, Cayley, Bootz, The Legible City), with the Eco pitfall that once you get the idea you stop reading.

### Ch. 10: Participatory interactivity in electronic media
- Virtual bodies: immersion plus interactivity needs body, actual or virtual, in the art-world. Walser: film says "look, I'll show you", spacemaker says "here, I'll help you discover".
- **Computer games**: distinguish world as rule set vs world as imaginary space. Text adventures give the user a recentered avatar ("remote-controlled extension of herself", Aarseth), more immersive than hypertext. Modern games are as much "being there" as "doing things". First vs third person. Games are meant to be beaten, art renews itself. Hybrid: Ceremony of Innocence.
- **MOOs**: apogee of creative make-believe, text as gesture, stage without script. Quality depends on the players.
- **Automated dialogue**: Eliza effect, pattern matching, alternative AI (credibility over cognition), **graceful degradation** (Julia changing subject to hockey), eccentric flat characters (Bates, Lyotard the cat), Humpty Dumpty as ideal AI character.
- **Interactive drama**: merges four functions (audience, character, actor, co-author). Needs both stage and script. Core paradox: free choice under a godlike designer. Characters follow a double logic (own goals plus plot). Learn from children's games: known script, voluntary cooperation.
- **VR themes**: alternative embodiment (flying, shrinking), epic exploration, Detour (Rita Addison).
- **Placeholder** (Laurel: ritual, epic, sense of place, critters embodiment, voice holders, Goddess in-world help) vs **Oz** (Aristotelian, actors plus director as proto drama manager, bus station scenario, 15 min). Key finding: interactors accept inconsistencies that observers notice, and want steering without feeling manipulated. Both make the system a game partner responsible for the user's pleasure, a classicist stance.
- Future: Alice-in-Wonderland narrativity, episodic little stories, with dialogue and metamorphosis. Main obstacle is economic (throughput).

## 5b. Mandatory papers

Read in full: Nilsson et al. 2016, Bruni et al. 2022, Bruni 2011. Not read in full (see the status list at the end of this section): Bruni and Baceviciute 2013, Murray 2018, Green and Jenkins 2014, Ryan 2015 (transmedia).

### Nilsson, Nordahl, Serafin (2016), Immersion revisited (Day 3)
Narrative literature review of immersion definitions (games, VEs, fiction) plus four presence theories. Fixes the "jungle of terms" from lecture 3.1.

- **Four views of immersion**: property of the system (Slater), perceptual response to the system (Witmer and Singer, McMahan perceptual, Ermi and Mayra sensory), response to narrative (Ryan, Adams and Rollings, Ermi and Mayra imaginative, Arsenault fictional, McMahan psychological/diegetic), response to challenges (Ermi and Mayra challenge-based, Adams and Rollings strategic and tactical, Ryan ludic, McMahan engagement/non-diegetic).
- **Their taxonomy, three orthogonal axes** (0 to 1 each, plotted as a cube): **system immersion** (objective), **narrative immersion** (spatial, temporal, emotional collapsed into one axis), **challenge-based immersion** (intellectual and sensorimotor, resembles flow). They drop "perceptual/sensory immersion" because it scales with system immersion and overlaps with presence (place illusion).
- Cube examples: (0,0,0) unmediated, empty scene. (1,0,0) novel. (0,0,1) sudoku. (1,0,1) tabletop RPG, no tech. (0,1,1) tech game, all challenge. (1,1,0) tech interactive narrative. (1,1,1) tech game with challenge and story.
- **Four presence theories**, each mapped onto the axes:
  - **Biocca 2003**, three-pole model: adds **mental imagery space** as a third pole beside physical and virtual. Explains presence when reading a book. Narrative immersion helps presence, challenges help by holding spatial attention.
  - **Waterworth and Waterworth 2001**, focus/locus/sensus of attention: presence vs **absence** depends on perceptual vs conceptual processing. Intellectual challenge causes absence, sensorimotor challenge supports presence.
  - **Riva et al. 2004**, layers of presence: proto (body), core (selective attention), extended (significance to self). Maximal presence needs all three aligned, so it needs both high system immersion and meaningful content. Narrative works if the user assumes a character role.
  - **Slater 2009**: **place illusion** (being there, from sensorimotor contingencies of the system) plus **plausibility illusion** (events feel real: system reacts to user actions, reacts on its own, is credible). Virtual body fuses both. Content and interest do not change place illusion, though narrative can discourage users from probing the system's limits.
- **Use**: state which immersion you mean, choose metrics to match (presence questionnaire vs narrative engagement vs flow).

### Bruni, Kadastik, Pedersen, Dini (2022), Digital narratives in extended realities (Days 4, 5, 6)
Core statements, useful for the Day 4 reading list and the oral.
- **Two immersion vocabularies**: XR means technological/perceptual (immersion = system, presence = response, Slater), IDN means **narrative immersion** with spatial, temporal, emotional, plus **epistemic immersion** ("the desire to know", mystery, clue assembly). Also proposes **social immersion** (believable social behaviour of agents).
- **IDN trajectories** (Koenitz): text-based (hypertext), audiovisual (interactive movies, installations), complex designs using AI and multimodality (games, XR).
- **Two levels of interactivity**: medium (viewpoint motion control, selection, manipulation, system control) vs narrative (internal/external, exploratory/ontological, plus Ryan's reactive, random selection, purposeful selection, productive). Main XR design challenge: **harmonize medium interactivity with narrative interactivity**.
- **Environmental storytelling**: environment is the discourse. **Embedded** narratives (clues, epistemic and spatial immersion) vs **emergent** narratives (actions and interpretation).
- **Agency**: cause and effect between user and world. **Avoid run-amuck agency** (user kills antagonist, erases drama). Mismatch between affordances and narrative closure lowers agency.
- **Embodiment and focalization**: XR is both disembodying (avatar takes agency) and body-centred. Genette (who speaks vs who sees), Bal (internal vs external focalization), perceptibles vs imperceptibles (thoughts are hard to convey in XR). **More agency means more internal focalization**, which falls into the narrative paradox. **Point-of-view technologies** (tracking the user's perspective).
- **Author-Audience Distance (AAD)** (Bruni and Baceviciute 2013): gap between narrative intended by author and perceived by user, a function of **narrative intelligibility** ("audience receives or generates meaning close to what is intended"). More freedom means more open interpretation. First-person active participants suit open/emergent stories (abstract). Spectator, external focalization suits **didascalic**/didactic stories.
- **Agents and AI**: NPCs and **drama managers** drive believability, uncanny valley, need human behaviour models.
- **Domains**: games (Lone Echo, Alyx, VR visual novels), serious games (Micro Kingdoms, pervasive games), culture (CHESS, EMOTIVE), healthcare, immersive journalism (NYT 2015, "empathy machine" critique).
- **Future**: narrative cognition in XR (enactive approach, biometrics), narratives as rhetorical devices, risk of manipulation, transmedia driven by XR, blurred ontological boundaries (fiction vs reality, fact vs fake), especially for children.

### Bruni (2011), Cognitive sustainability in the age of digital culture (Day 6, ethics)
- Bateson: ecological crisis from three mutually reinforcing loops: population, technology, **hubris**. Can steer technology, not stop it.
- **Technosphere** (global digital infrastructure, "DIIRT": digital-interactive-immersive-representational technologies) vs **semiosphere** (Lotman: the whole semiotic space of culture, diachronic and synchronic, heterogeneous), embedded in the biosphere.
- Technosphere expands reach into the semiosphere, but more is not better. Risks: information overflow, attention economy, homogenization, "pollution" of cognition.
- Two meanings of "intelligent technosphere": adaptive/context-aware vs designed to encourage sustainability and social responsibility. Persuasive technology should build sustainability in from the start.
- Ethical challenge: not to make technology autotelic, but to handle **content** and cultural legitimacy. Information is not knowledge.

### Not read in full, what is known
| Paper | Status | What I have |
|---|---|---|
| Bruni and Baceviciute 2013, Narrative intelligibility and closure | paywalled (Springer) | Abstract only: AAD as function of narrative intelligibility, complemented by narrative closure, abstract vs didascalic (figurativeness), distinction between system goals and narrative goals, two levels (system, embedded narrative). Fuller definitions come only through Bruni 2022 above. Lecture 5 also uses AAD. |
| Murray 2018, Kaleidoscopic view | author copy exceeds my 50 MB fetch limit, other copies login-walled | Abstract-level only (evolution of IDN as its own field, IDN needs independent study, Oz group 1993, focus on interaction and procedurality). |
| Green and Jenkins 2014, Interactive narratives | paywalled (Wiley) | Abstract only: interactive narratives increase user control and loosen narrative structure, with effects on engagement (transportation, identification, perceived realism), responsibility, and outcomes (enjoyment, attitude change, health behaviour). Framework aimed at entertainment-education. |
| Ryan 2015, Transmedia storytelling: buzzword or new experience? | JSTOR/MUSE captcha, not bypassed | Not read. Ryan's related 2013 article (Poetics Today, "Transmedial storytelling and transfictionality") was found but is a different paper. |

## 6. Companion chapters (recap)

- **Ryan, ch. 2**: fuzzy-set narrativity, 8 conditions, story as mental representation, fictionality matters more than narrativity.
- **Montfort, ch. 12**: data vs program, interactive, single vs multi-user, single vs multi-channel. Levels: directive/report (extradiegetic), command/reply (diegetic PRISM), nested simulation (hypodiegetic Perry Simm). Simulated plus narrated characters matter more than cut-scene only ones.
- Other chapters unread: use as glossary (Herman, Abbott, Bridgeman, Margolin, Jahn, Mittell, Phelan, etc.).

## 7. Cross-cutting ideas for the exam and mini-project

- **Immersion vs interactivity**: they trade off. World metaphor (immersion) vs game metaphor (interactivity). Reconcile by putting the body in the world (internal-ontological forms, VR, games, MOOs, interactive drama).
- **Coherence vs freedom** (narrative paradox): connectivity is inversely proportional to story quality. Options: tree, flow chart with memory, braided plot, action space, hidden story, emergent narrative, drama manager (Oz, Day 5), microevents.
- **Three immersions** map to setting, plot, character. Diagnose failures by layer (technological, perceptual, narrative).
- **Design levers**: pick interactivity type (Ryan 2000 and 2015), pick link structure, decide where to be internal or external, decide what memory the system keeps, add local delight, provide in-world help (Goddess) or a known script, use graceful degradation and eccentric characters for AI agents.
- **Critical vocabulary**: story/discourse, fabula/syuzhet, diegesis, narrativity scale, recentering, transportation, make-believe, mental simulation, ergodic, aporia/epiphany, Eliza effect, emergent narrative.
- **Ethics/rhetoric** (Day 6): Interactive narratives as persuasion (Green and Jenkins), transmedia, responsibility. Not covered in the material read so far.

## 8. Edition mapping (NVR1 2001 vs NVR2 2015)

| Topic | NVR1 2001 | NVR2 2015 |
|---|---|---|
| Faces of the virtual | ch. 1 | ch. 1 |
| VR as dream and technology | ch. 2 | ch. 2 |
| Theories of immersion | ch. 3 | ch. 3 |
| Spatial, temporal, emotional immersion | ch. 4, 5 | ch. 4 (Varieties of Immersion) |
| World vs game | ch. 6 | ch. 5 |
| Games without worlds (dysfunctionality as play) | (in ch. 6) | ch. 6 (Texts without Worlds) |
| Forms of interactivity | (in ch. 7, 8) | ch. 7 (The Many Forms of Interactivity) |
| Hypertext | ch. 7 | ch. 8 (Exploratory, Extensional Interactivity) |
| Coherence and selective interactivity | ch. 8 | no separate chapter, syllabus offers NVR2 ch. 7 as the alternative |
| Participatory interactivity | ch. 9, 10 | ch. 9 and ch. 10 (Chasing the Dream of the Immersive, Interactive Narrative) |

Built from the NVR2 table of contents (OCR noisy) and the Moodle reading list. Rows marked "in ch." are inferred, verify against the book before citing.

## 9. Open items

- Day 4 and Day 6 decks are still missing from the folder (Day 4 XR content only appears as slides 36 to 40 of deck 5.2). Download and add them.
- Mini-project deadline: not given in the guidelines or template, only "announced on Moodle". Confirm the date.
- Get full text of Bruni and Baceviciute 2013, Green and Jenkins 2014, Ryan 2015 transmedia (AAU library login) and Murray 2018 (author PDF is over 50 MB).
- Read Ryan ch. 1, 2, 4, 5, 7, 9 in full if the oral covers them.
- Fix the mini-project domain, then map choices onto the frameworks above.
