#!/bin/sh
case "$GIT_AUTHOR_NAME" in
  "Cursor Agent"|"cursoragent"|"CursorAgent") export GIT_AUTHOR_NAME="Mmrzjavv"; export GIT_AUTHOR_EMAIL="mohammad.r.javaheri@gmail.com";;
esac
case "$GIT_COMMITTER_NAME" in
  "Cursor Agent"|"cursoragent"|"CursorAgent") export GIT_COMMITTER_NAME="Mmrzjavv"; export GIT_COMMITTER_EMAIL="mohammad.r.javaheri@gmail.com";;
esac
