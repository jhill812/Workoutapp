# End-to-end tests (Playwright)

Drives a published build on an emulated Pixel 7, served under `/Workoutapp/` the way GitHub Pages serves it.

```bash
sed -i 's|<base href="/" />|<base href="/Workoutapp/" />|' src/WorkoutTracker/wwwroot/index.html
dotnet publish src/WorkoutTracker -c Release -o out
git checkout src/WorkoutTracker/wwwroot/index.html
mkdir -p site && cp -r out/wwwroot site/Workoutapp && cp site/Workoutapp/index.html site/Workoutapp/404.html
node tests/e2e/server.js site &                 # requires: npm i playwright
mkdir -p shots && node tests/e2e/e2e.js shots   # screenshots land in ./shots
```
