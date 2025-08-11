# Import

## 1. FivetranConnectionSupport.cs
- Dodanie walidacji apiKey oraz apiSecret w metodzie GetConnectionDetailsForSelection() aby zapobiec utworzeniu nieprawidłowego połączenia
- Poprawienie obsługi błędów (informacje przekazywane w wyjątkach)
- Modyfikacja metody SelectToImport korzystając z ToList() do pobrania grup w celu uniknięcia wielokrotnej enumeracji kolekcji
- Wykorzystanie StringBuilder zamiast += stringów w pętlach
- Użycie property groups.Count zamiast dodatkowej enumeracji poprzez korzystanie z groups.Count()
- Refactor metody RunImport na wywołanie asynchroniczne poprzez Task RunImportAsync oraz zastosowano Parallel.ForEachAsync dla równoległego pobierania danych

## 2. RestApiManagerWrapper.cs
- Dodanie throw new ArgumentNullException przy nieprawidłowych argumentach
- Dodanie pola _disposed oraz modyfikacja metody Dispose() w celu zabezpieczenia przed wielokrotnym zwalnianiem zasobów i potencjalnymi błędami z tym związanych

# FivetranClient

## 1. Models/*
- Inicjalizacja pól typu string za pomocą string.Empty aby uniknąć potencjalnych null reference
- Inicjalizacja kolekcji bezpośrednio w definicjach co ma zabezpieczyć przed null reference exception

## 2. HttpRequestHandler.cs
- Poprawienie błędu initjalizacji semafory (w przypadku gdy podana była wartość maxConcurrentRequests > 0 to i tak startowała z 0 dostępnymi miejscami, więc wszystkie wątki byłyby zablokowane na WaitAsync()
- Poprawienie metody _GetAsync() aby uniknąć rekurencji w przypadku błędu 429. Zastosowana pętla z kontrolą czasu oraz poprawne zwalnianie semafory w końcowej fazie

## 3. RestApiManager.cs
- Dodanie pola _disposed oraz modyfikacja metody Dispose() w celu zabezpieczenia przed wielokrotnym zwalnianiem zasobów i potencjalnymi błędami z tym związanych

## 4. TtlDictionary.cs
- Zmiana Dictionary na ConcurrentDictionary dla współbieżności
- Zastosowanie pól Value oraz ExpiryDateTime dla wartości w krotce, w celu przejrzystości kodu (aby nie stosować entry.Item1, entry.Item2)
- Niepotrzebne użycie typu T w metodzie GetOrAddAsync
- We wszystkich zaimplementowanych metodach zastosowanie dwóch warunków w jednym if razem zamiast pojedynczo

## 5. FivetranHttpClient.cs
- Dodana walidacja konstruktora na parametry baseAddress, apiKey i apiSecret aby nie tworzyć klienta z pustymi danymi
