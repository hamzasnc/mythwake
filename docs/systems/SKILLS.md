# Skill upgrades – concept

Source: design doc, "Hero Upgrade System" (Skills: improves basic attacks, passives, normal skills and ultimates;
short-term progression: level up, equip better gear, upgrade skills) and "Trial of Fragments" rewards ("Upgrade
stones"). Numbers are first-pass balancing (`Content/Data/skills.json`).

- Normal Skill, Special Skill, Special Skill II and Ultimate have a level each (start 1).
- Each level above 1 adds **+8 %** to that skill's damage, heals and shields.
- Max level = 3 + the hero's stars (1★: 4 … 7★: 10).
- Cost from level L: 5 × L **Upgrade stones** and 2000 × L² Gold.
- Upgrade stones (the doc's name, listed as a Trial of Fragments reward): every Trial floor, campaign boss stages,
  the 60-point daily chest and the General Shop.
- Not yet: passive upgrades (passives are mostly stat components; they grow with Awakening and Fragments instead).
