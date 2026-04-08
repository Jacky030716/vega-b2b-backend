UPDATE usr."StudentCredentials" SET "VisualPasswordHash" = 'DEFAULT';
SELECT "Id", "StudentLoginCode", "VisualPasswordHash" FROM usr."StudentCredentials";
