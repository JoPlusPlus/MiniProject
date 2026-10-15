# 1. Why are the sync and async times almost the same when orders are awaited one after another?
Each await finishes before the next order starts so the work is still one order at a time. Async only frees the thread while waiting so it doesnt loop faster

# 2. Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence.
no they are not, because the ThreadPool may send another thread to continue the work not always the same thread
`[17:33:09.276] [Thread 12] OrderValidator -> Validating order #8` 
`[17:33:12.330] [Thread 13] OrderRepository -> Order #8 saved (Validated)` 
the validation started at thread 12 and ended by thread 13

# 3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?
while `Task.delay()` it returns to ThreadPool and execute another work, while `Thread.Sleep()` blocks the thread 

# 4. Why should a repository method return Task<T> instead of T?
we return Task so that we can use await on it

# 5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?
`File.ReadAllText` -> thread is blocked while reading
`File.ReadAllTextAsync` -> thread is free to do another work until reading finish
