## Bugs:
- Bugs introduced for debugging
  - Snapshot.cs pulls data from Directory_LibraryData
    - Because only current data is TSD (not TSDA), and I don't plan on using TSD permanently, so I am not implementing a place for it in the data library
  - Snapshot.cs uses DailyValue (not adjusted!) values