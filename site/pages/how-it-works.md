# How it works

Each ecosystem's repository maintains itself through agent skills, and nothing reaches an opinion without passing CI and the owner's review. The diagrams are orientation only: the skills and the roster's admission criteria, linked from each ecosystem's pages, are canonical.

## Every change is a pull request

Each skill works on its own branch and lands through a pull request. CI checks the metadata on every opinion and template, that every cited source is on the roster and allowed to cite, and that every link resolves. The owner then reviews and merges. Two skills only report: `audit-freshness` finds drifted review dates and lagging version pins, and `verify-project` grades an external codebase against the opinions. Their findings, a scheduled release watch and a monthly harvest reminder start the next run.

![Maintenance pipeline: each skill from what starts it, through CI and the owner's review, to the roster, research/, or the opinions and templates; audit-freshness and verify-project only report](assets/diagrams/maintenance-pipeline.svg)

## Opinions have to be earned

A source joins the roster on its track record: two years of sustained writing at minimum, plus depth, accuracy and independence of signal. Under two years, or dormant for over a year, is the watch list, which supplies leads but is never cited. Two markings narrow what a citable source may carry, and standing is re-vetted whenever a source goes quiet or a blocker clears.

![Source standing: vet-source sorts a candidate into citable, watch list or declined, and a citable source is unmarked, Corroborate or Discovery-only](assets/diagrams/source-standing.svg)

## Research is staged, never merged in place

A research topic saves the evidence on a subject in its own pull request. A second pull request then promotes it into the opinions and templates or discards it, deleting the topic either way, so a topic still on disk is unresolved by definition.

![Research lifecycle: research-topic saves research/{topic}.md, and resolve-research promotes, discards or partly promotes it](assets/diagrams/research-lifecycle.svg)

## House opinions

One human outranks the roster: each repository's owner. Their own preferences are recorded separately and always visibly marked **House:** in place, so a reader can tell community practice from local convention.
