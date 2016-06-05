{{}}

## taxonomies (many-to-many)

- **executed**:  
  - on config.sban change: reset all and rerun all
  - on page change: rerun all 
- **dependencies**:
  - config.sban (and extends) for the list of taxonomies
  - iterate on all pages after loading, collect taxonomies
  - generate dynamic pages (each term and terms list) 

## bundle (many-to-many)

- **executed**:
  - on config.sban change: reset all bundles and rerun 
- **dependencies**: 
  - config.sban for the list of bundles 
  - each bundle, a list of css/js files
  - each bundle, config that determines the output (concat -> many-to-one)

## content (1-to-1)
- **executed**:
  - on config.sban change (reset all pages)
  - on layout change
  - on new page/page modified
- **dependencies**:
  - on config.sban
  - on any variables (access to taxonomies or access to pages)
  - on type of content
- **actions**:
  - convert md to html (requires front matter) 
  - layout a content (requires front matter)
  - convert scss to css

## Full Build Workflow

from a clean repo (no previous `.lunet/deps.txt` and `.lunet/www`)

1. Load config (and extends)
2. Load all content
   - static files
   - pages (parse, run without layout, extract summary)
3. Run content group processors on all content 
   - may produce dynamic content (static files, new pages)
   - may discard content (static files, pages)
   - may squash multiple content into a new content (discard + produce)
   - may use transform processors to modify content
   - note: some content processors can run concurrently, others serially
4. Run content processors for each content
   - For static files:
     - transform processors + copy
   - For pages and dynamic pages:
     - layout + transform processors + copy

> Note: Due to the nature of the data and dependencies, each step requires to wait for the completion of the previous step (1->2->3->4)
> But inside a step, parallelism can occur depending on the step
> Steps 3 and 4 are updating file dependencies for each content
> Dependencies are stored in a text file at `.lunet/deps.txt`

The dependency file contains the following info:

```
#processors // (list of group processors ids)
id GUID
...
#inputs 
id timestamp type input_file_path  // (type is S: static file, P: page, A: assembly)
...
#outputs
config/output_file_path [@processor_id]+ [inputs_id]* 
```

## Incremental Build Workflow

After the full build workflow happened.

- static file/page: modified/deleted/added
  - Rerun partially 2,3,4 only for the pages modified (TODO: Add details for each case)
  - Dependencies must be updated
- layout/include: modified/deleted/added
  - Rerun partially 4 on content using the specified layout (including dependencies)
- config or extends: modified/deleted 
  - Rerun a full build


- a change in a css used by a bundle would not regenerate pages, just the bundle
- a change in bundle config would regenerate all pages








