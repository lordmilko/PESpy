## Visual C++ 4.0 Test Files

File -> New -> Project Workspace -> Console Application
File -> New -> Text File -> main.cpp
Insert -> Files Into Project -> main.cpp

(note that you can see the files in the project by switching the sidebar on the left from ClassView to FileView)

Enter in the following text

```c
#include <stdio.h>

int main(int argc, char** argv)
{
    printf("hello world\n");
    return 0;
}
```

Build -> Rebuild All