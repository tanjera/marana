
TO_DO.txt




--> Combine all projects into one WPF form
	- Use tabs for different functions (charts, library update, data aggregation)






*** _Marana (Classes)

- On httprequest instead of “if response ==“ for error catching, use “while response ==“ or for() and reattempt loop for X iterations
- error test httprequest for made up erroneous symbol






*** Aggregator

Saves all daily data to a library folder (each JSON object to a different file)
Can aggregate data without needing to download (unless library out of date).

* %^ values compare to this week’s mean?
* add timespans for this -7 days, -30 days
- Console.write # / total (1/3500) per symbol

* Refactor into functions
- Aggregator main menu
	- press 1 to iterate nasdaq
	- press 2 to select symbols
- Add equations to aggregator comments by symbol key






*** Visualizer
	- Search functionality to ListView
	- Select functionality to ListView (and request call, add to chart series)
